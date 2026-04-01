using Microsoft.Win32;

namespace TaskNoteTracker.Services;

public class StartupService
{
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TaskNoteTracker";

    public void EnsureEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        if (key?.GetValue(AppName) is null)
            key?.SetValue(AppName, Environment.ProcessPath ?? string.Empty);
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        return key?.GetValue(AppName) is not null;
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
