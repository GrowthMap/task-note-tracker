namespace TaskNoteTracker.Services;

public static class TimezoneService
{
    private static readonly TimeZoneInfo BdtZone =
        TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");

    private static readonly TimeZoneInfo CentralZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    public static DateTime ToBdt(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), BdtZone);

    public static DateTime ToCdt(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), CentralZone);
}
