using FluentAssertions;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tests.Helpers;

namespace TaskNoteTracker.Tests.Services;

public class TaskTypeServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly TaskNoteTracker.Data.AppDbContext _db;
    private readonly TaskTypeService _service;

    public TaskTypeServiceTests()
    {
        (_db, _conn) = DbFactory.Create();
        _service = new TaskTypeService(_db);
    }

    [Fact]
    public async Task AddAsync_StoresTaskType()
    {
        await _service.AddAsync("Deep Work");
        var all = await _service.GetAllAsync();
        all.Should().ContainSingle(t => t.Name == "Deep Work");
    }

    [Fact]
    public async Task AddAsync_TrimsWhitespace()
    {
        await _service.AddAsync("  Meeting  ");
        var all = await _service.GetAllAsync();
        all.Should().ContainSingle(t => t.Name == "Meeting");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSortedByName()
    {
        await _service.AddAsync("Zeta");
        await _service.AddAsync("Alpha");
        var all = await _service.GetAllAsync();
        all.Select(t => t.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task CanDeleteAsync_ReturnsTrueWhenNoEntries()
    {
        var type = await _service.AddAsync("Admin");
        (await _service.CanDeleteAsync(type.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteAsync_ReturnsFalseWhenEntriesExist()
    {
        var type = await _service.AddAsync("Break");
        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Activity = "Coffee",
            TaskTypeId = type.Id
        });
        await _db.SaveChangesAsync();

        (await _service.CanDeleteAsync(type.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskType_WhenNoEntries()
    {
        var type = await _service.AddAsync("Research");
        await _service.DeleteAsync(type.Id);
        (await _service.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenEntriesExist()
    {
        var type = await _service.AddAsync("Work");
        _db.TaskEntries.Add(new TaskEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Activity = "Coding",
            TaskTypeId = type.Id
        });
        await _db.SaveChangesAsync();

        var act = async () => await _service.DeleteAsync(type.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*entries*");
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
