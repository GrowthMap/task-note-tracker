namespace TaskNoteTracker.Services;

public sealed class TrackingTimer(TimeSpan interval) : IDisposable
{
    private System.Timers.Timer? _timer;

    public event EventHandler? IntervalElapsed;
    public bool IsRunning => _timer?.Enabled ?? false;

    public void Start()
    {
        _timer?.Dispose();
        _timer = new System.Timers.Timer(interval.TotalMilliseconds) { AutoReset = false };
        _timer.Elapsed += OnElapsed;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public void Reset()
    {
        _timer?.Stop();
        _timer?.Start();
    }

    private void OnElapsed(object? sender, System.Timers.ElapsedEventArgs e) =>
        IntervalElapsed?.Invoke(this, EventArgs.Empty);

    public void Dispose() => Stop();
}
