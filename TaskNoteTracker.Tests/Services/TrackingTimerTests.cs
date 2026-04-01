using FluentAssertions;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class TrackingTimerTests
{
    [Fact]
    public async Task Start_FiresIntervalElapsedAfterInterval()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fired = false;
        timer.IntervalElapsed += (_, _) => fired = true;

        timer.Start();
        await Task.Delay(250);

        fired.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_PreventsEventFromFiring()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fired = false;
        timer.IntervalElapsed += (_, _) => fired = true;

        timer.Start();
        timer.Stop();
        await Task.Delay(250);

        fired.Should().BeFalse();
    }

    [Fact]
    public async Task IntervalElapsed_FiresExactlyOnce_WithoutReset()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fireCount = 0;
        timer.IntervalElapsed += (_, _) => fireCount++;

        timer.Start();
        await Task.Delay(400);

        fireCount.Should().Be(1);
    }

    [Fact]
    public async Task Reset_AllowsTimerToFireAgain()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMilliseconds(100));
        var fireCount = 0;
        timer.IntervalElapsed += (_, _) => { fireCount++; timer.Reset(); };

        timer.Start();
        await Task.Delay(450);

        fireCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void IsRunning_FalseBeforeStart()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        timer.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void IsRunning_TrueAfterStart()
    {
        using var timer = new TrackingTimer(TimeSpan.FromMinutes(30));
        timer.Start();
        timer.IsRunning.Should().BeTrue();
        timer.Stop();
    }
}
