using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data.Models;

namespace TaskNoteTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskType> TaskTypes => Set<TaskType>();
    public DbSet<TaskEntry> TaskEntries => Set<TaskEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntry>()
            .HasOne(e => e.TaskType)
            .WithMany(t => t.Entries)
            .HasForeignKey(e => e.TaskTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
