namespace TaskNoteTracker.Data.Models;

public class TaskEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int TaskTypeId { get; set; }
    public TaskType TaskType { get; set; } = null!;
}
