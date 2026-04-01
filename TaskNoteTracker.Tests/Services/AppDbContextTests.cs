using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Tests.Helpers;

namespace TaskNoteTracker.Tests.Services;

public class AppDbContextTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly TaskNoteTracker.Data.AppDbContext _db;

    public AppDbContextTests()
    {
        (_db, _conn) = DbFactory.Create();
    }

    [Fact]
    public void CanSaveAndRetrieveTaskType()
    {
        _db.TaskTypes.Add(new TaskType { Name = "Deep Work" });
        _db.SaveChanges();

        _db.TaskTypes.Single().Name.Should().Be("Deep Work");
    }

    [Fact]
    public void CanSaveAndRetrieveTaskEntry()
    {
        var taskType = new TaskType { Name = "Meeting" };
        _db.TaskTypes.Add(taskType);
        _db.SaveChanges();

        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
            Activity = "Sprint planning",
            Notes = "Q2 goals",
            TaskTypeId = taskType.Id
        });
        _db.SaveChanges();

        var entry = _db.TaskEntries.Include(e => e.TaskType).Single();
        entry.Activity.Should().Be("Sprint planning");
        entry.TaskType.Name.Should().Be("Meeting");
    }

    [Fact]
    public void DeleteTaskType_WhenEntriesExist_Throws()
    {
        var taskType = new TaskType { Name = "Work" };
        _db.TaskTypes.Add(taskType);
        _db.SaveChanges();

        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Activity = "Coding",
            TaskTypeId = taskType.Id
        });
        _db.SaveChanges();

        var act = () => { _db.TaskTypes.Remove(taskType); _db.SaveChanges(); };
        act.Should().Throw<Exception>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
