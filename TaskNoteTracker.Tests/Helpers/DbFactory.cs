using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;

namespace TaskNoteTracker.Tests.Helpers;

public static class DbFactory
{
    public static (AppDbContext db, SqliteConnection conn) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return (db, connection);
    }
}
