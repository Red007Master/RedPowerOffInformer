public static class Clock
{
    public static TimeSpan TimeZoneOffset { get; set; } = TimeSpan.FromHours(0); //Backup
    public static TimeZoneInfo? TimeZone { get; set; } = null;

    public static DateTime Now => GetCurrentTime();

    private static DateTime GetCurrentTime()
    {
        if (TimeZone is null)
        {
            return DateTime.UtcNow + TimeZoneOffset;
        }
        else
        {
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZone);
        }
    }
}