using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Services;

public record EntryFilter(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int? TaskTypeId = null,
    string? SearchText = null
);

public class EntryService(AppDbContext db)
{
    public async Task<TaskEntry> CreateAsync(
        DateTime timestampUtc, string activity, string? notes, int taskTypeId)
    {
        var entry = new TaskEntry
        {
            TimestampUtc = timestampUtc,
            Activity = activity.Trim(),
            Notes = notes?.Trim(),
            TaskTypeId = taskTypeId
        };
        db.TaskEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task UpdateAsync(int id, string activity, string? notes, int taskTypeId)
    {
        var entry = await db.TaskEntries.FindAsync(id)
            ?? throw new InvalidOperationException($"Entry {id} not found.");
        entry.Activity = activity.Trim();
        entry.Notes = notes?.Trim();
        entry.TaskTypeId = taskTypeId;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entry = await db.TaskEntries.FindAsync(id)
            ?? throw new InvalidOperationException($"Entry {id} not found.");
        db.TaskEntries.Remove(entry);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TaskEntry>> GetFilteredAsync(EntryFilter filter)
    {
        var query = db.TaskEntries.Include(e => e.TaskType).AsQueryable();

        if (filter.FromUtc.HasValue)
            query = query.Where(e => e.TimestampUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue)
            query = query.Where(e => e.TimestampUtc <= filter.ToUtc.Value);
        if (filter.TaskTypeId.HasValue)
            query = query.Where(e => e.TaskTypeId == filter.TaskTypeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var text = $"%{filter.SearchText.Trim()}%";
            query = query.Where(e =>
                EF.Functions.Like(e.Activity, text) ||
                (e.Notes != null && EF.Functions.Like(e.Notes, text)));
        }

        return await query.OrderByDescending(e => e.TimestampUtc).ToListAsync();
    }

    public async Task<TaskEntry?> GetByIdAsync(int id) =>
        await db.TaskEntries.Include(e => e.TaskType).FirstOrDefaultAsync(e => e.Id == id);
}
