using FluentAssertions;
using TaskNoteTracker.Data.Models;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tests.Helpers;

namespace TaskNoteTracker.Tests.Services;

public class EntryServiceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _conn;
    private readonly TaskNoteTracker.Data.AppDbContext _db;
    private readonly EntryService _service;
    private readonly TaskType _taskType;

    public EntryServiceTests()
    {
        (_db, _conn) = DbFactory.Create();
        _service = new EntryService(_db);
        _taskType = new TaskType { Name = "Work" };
        _db.TaskTypes.Add(_taskType);
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_SavesEntry()
    {
        var utc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var entry = await _service.CreateAsync(utc, "Writing tests", "notes here", _taskType.Id);

        entry.Id.Should().BeGreaterThan(0);
        entry.Activity.Should().Be("Writing tests");
        entry.Notes.Should().Be("notes here");
        entry.TaskTypeId.Should().Be(_taskType.Id);
    }

    [Fact]
    public async Task CreateAsync_TrimsActivity()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "  Coding  ", null, _taskType.Id);
        entry.Activity.Should().Be("Coding");
    }

    [Fact]
    public async Task UpdateAsync_ChangesFields()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "Old activity", null, _taskType.Id);
        var otherType = new TaskType { Name = "Break" };
        _db.TaskTypes.Add(otherType);
        await _db.SaveChangesAsync();

        await _service.UpdateAsync(entry.Id, "New activity", "new notes", otherType.Id);

        var updated = await _db.TaskEntries.FindAsync(entry.Id);
        updated!.Activity.Should().Be("New activity");
        updated.Notes.Should().Be("new notes");
        updated.TaskTypeId.Should().Be(otherType.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntry()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "Delete me", null, _taskType.Id);
        await _service.DeleteAsync(entry.Id);
        _db.TaskEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByDateRange()
    {
        await _service.CreateAsync(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), "March entry", null, _taskType.Id);
        await _service.CreateAsync(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), "April entry", null, _taskType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter(
            FromUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        results.Should().ContainSingle(e => e.Activity == "April entry");
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByTaskType()
    {
        var otherType = new TaskType { Name = "Break" };
        _db.TaskTypes.Add(otherType);
        await _db.SaveChangesAsync();

        await _service.CreateAsync(DateTime.UtcNow, "Work entry", null, _taskType.Id);
        await _service.CreateAsync(DateTime.UtcNow, "Break entry", null, otherType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter(TaskTypeId: otherType.Id));
        results.Should().ContainSingle(e => e.Activity == "Break entry");
    }

    [Fact]
    public async Task GetFilteredAsync_SearchesActivityAndNotes()
    {
        await _service.CreateAsync(DateTime.UtcNow, "Reviewing PRs", null, _taskType.Id);
        await _service.CreateAsync(DateTime.UtcNow, "Coding", "see the PR for context", _taskType.Id);
        await _service.CreateAsync(DateTime.UtcNow, "Lunch", null, _taskType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter(SearchText: "PR"));
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredAsync_ReturnsNewestFirst()
    {
        await _service.CreateAsync(new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc), "First", null, _taskType.Id);
        await _service.CreateAsync(new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc), "Second", null, _taskType.Id);

        var results = await _service.GetFilteredAsync(new EntryFilter());
        results[0].Activity.Should().Be("Second");
        results[1].Activity.Should().Be("First");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectEntry()
    {
        var entry = await _service.CreateAsync(DateTime.UtcNow, "Find me", null, _taskType.Id);
        var found = await _service.GetByIdAsync(entry.Id);
        found!.Activity.Should().Be("Find me");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var found = await _service.GetByIdAsync(9999);
        found.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithBdtConversion_StoresCorrectUtc()
    {
        var bdt = new DateTime(2026, 4, 3, 12, 0, 0);
        var timestampUtc = TimezoneService.ToUtc(bdt);

        var entry = await _service.CreateAsync(timestampUtc, "BDT entry", null, _taskType.Id);

        entry.TimestampUtc.Should().Be(new DateTime(2026, 4, 3, 6, 0, 0, DateTimeKind.Utc));
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
