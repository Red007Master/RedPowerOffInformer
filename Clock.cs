public static class Clock
{
    // public static TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    public static TimeSpan TimeZoneOffset { get; set; } = TimeSpan.FromHours(0);

    public static DateTime Now => DateTime.UtcNow + TimeZoneOffset;
}