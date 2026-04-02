using FluentAssertions;
using TaskNoteTracker.Services;

namespace TaskNoteTracker.Tests.Services;

public class TimezoneServiceTests
{
    [Fact]
    public void ToBdt_ConvertsUtcCorrectly()
    {
        // BDT is UTC+6, no DST
        var utc = new DateTime(2026, 4, 1, 6, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToBdt(utc).Should().Be(new DateTime(2026, 4, 1, 12, 0, 0));
    }

    [Fact]
    public void ToBdt_MidnightUtcIsEarlyMorningBdt()
    {
        var utc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToBdt(utc).Should().Be(new DateTime(2026, 1, 15, 6, 0, 0));
    }

    [Fact]
    public void ToCdt_ConvertsUtcDuringDaylightSavingTime()
    {
        // CDT (summer) = UTC-5
        var utc = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToCdt(utc).Should().Be(new DateTime(2026, 7, 1, 7, 0, 0));
    }

    [Fact]
    public void ToCdt_ConvertsUtcDuringStandardTime()
    {
        // CST (winter) = UTC-6
        var utc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        TimezoneService.ToCdt(utc).Should().Be(new DateTime(2026, 1, 15, 6, 0, 0));
    }

    [Fact]
    public void ToUtc_ConvertsBdtToCorrectUtc()
    {
        // BDT is UTC+6, no DST — midday BDT should be 06:00 UTC
        var bdt = new DateTime(2026, 4, 3, 12, 0, 0);
        TimezoneService.ToUtc(bdt).Should().Be(new DateTime(2026, 4, 3, 6, 0, 0, DateTimeKind.Utc));
    }
}
