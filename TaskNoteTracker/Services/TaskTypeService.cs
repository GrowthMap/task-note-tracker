using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Services;

public class TaskTypeService(AppDbContext db)
{
    public async Task<IReadOnlyList<TaskType>> GetAllAsync() =>
        await db.TaskTypes.OrderBy(t => t.Name).ToListAsync();

    public async Task<TaskType> AddAsync(string name)
    {
        var taskType = new TaskType { Name = name.Trim() };
        db.TaskTypes.Add(taskType);
        await db.SaveChangesAsync();
        return taskType;
    }

    public async Task<bool> CanDeleteAsync(int id) =>
        !await db.TaskEntries.AnyAsync(e => e.TaskTypeId == id);

    public async Task DeleteAsync(int id)
    {
        var taskType = await db.TaskTypes.FindAsync(id)
            ?? throw new InvalidOperationException($"TaskType {id} not found.");

        if (await db.TaskEntries.AnyAsync(e => e.TaskTypeId == id))
            throw new InvalidOperationException(
                "Cannot delete a task type that has entries.");

        db.TaskTypes.Remove(taskType);
        await db.SaveChangesAsync();
    }
}
