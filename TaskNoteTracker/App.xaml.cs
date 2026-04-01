using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TaskNoteTracker.Data;
using TaskNoteTracker.Services;
using TaskNoteTracker.Tray;

namespace TaskNoteTracker;

public partial class App : Application
{
    private TrayManager? _trayManager;
    private AppDbContext? _db;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskNoteTracker");
        Directory.CreateDirectory(dataDir);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource={Path.Combine(dataDir, "tracker.db")}")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.Migrate();

        new StartupService().EnsureEnabled();

        _trayManager = new TrayManager(_db);
        _trayManager.ExitRequested += (_, _) => Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        _db?.Dispose();
        base.OnExit(e);
    }
}
