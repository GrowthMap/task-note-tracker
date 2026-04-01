using System.Drawing;
using System.Windows.Forms;
using TaskNoteTracker.Data;
using TaskNoteTracker.Services;
using TaskNoteTracker.Windows;

namespace TaskNoteTracker.Tray;

public sealed class TrayManager : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TrackingTimer _timer;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private TrackingPopupWindow? _popup;
    private bool _isTracking;

    public event EventHandler? ExitRequested;

    public TrayManager(AppDbContext db)
    {
        _db = db;
        _timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        _timer.IntervalElapsed += OnTimerElapsed;

        _toggleItem = new ToolStripMenuItem("Start Tracking", null, OnToggleTracking);
        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open History", null, OnOpenHistory);
        menu.Items.Add("Manage Task Types", null, OnManageTaskTypes);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, OnExit);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Task Note Tracker — Not tracking",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private void OnToggleTracking(object? sender, EventArgs e)
    {
        if (_isTracking) StopTracking();
        else StartTracking();
    }

    private void StartTracking()
    {
        _isTracking = true;
        _toggleItem.Text = "Stop Tracking";
        _notifyIcon.Text = "Task Note Tracker — Tracking";
        _timer.Start();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_popup != null) return;
            _popup = new TrackingPopupWindow(_db);
            _popup.Closed += OnPopupClosed;
            _popup.Show();
        });
    }

    private void StopTracking()
    {
        _isTracking = false;
        _toggleItem.Text = "Start Tracking";
        _notifyIcon.Text = "Task Note Tracker — Not tracking";
        _timer.Stop();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            _popup?.Close();
            _popup = null;
        });
    }

    private void OnTimerElapsed(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_popup != null) return;
            _popup = new TrackingPopupWindow(_db);
            _popup.Closed += OnPopupClosed;
            _popup.Show();
        });
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _popup = null;
        if (_isTracking) _timer.Reset();
    }

    private void OnOpenHistory(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new HistoryWindow(_db).Show());

    private void OnManageTaskTypes(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            new TaskTypeManagerWindow(_db).ShowDialog());

    private void OnExit(object? sender, EventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        _timer.Dispose();
        _notifyIcon.Dispose();
    }
}
