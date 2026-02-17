public static class Clock
{
    public static TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    
    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZone);
}