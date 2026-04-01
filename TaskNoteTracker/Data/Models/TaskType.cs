namespace TaskNoteTracker.Data.Models;

public class TaskType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<TaskEntry> Entries { get; set; } = [];
}
