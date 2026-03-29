using System;

namespace ProductivityTimer.Services;

public sealed class MoscowTimeProvider : ITimeProvider
{
    private readonly TimeZoneInfo _moscowTimeZone = CreateMoscowTimeZone();

    public DateTime Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _moscowTimeZone).DateTime;

    public DateTime Today => Now.Date;

    private static TimeZoneInfo CreateMoscowTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return CreateFallbackTimeZone();
        }
        catch (InvalidTimeZoneException)
        {
            return CreateFallbackTimeZone();
        }
    }

    private static TimeZoneInfo CreateFallbackTimeZone()
    {
        return TimeZoneInfo.CreateCustomTimeZone(
            "MoscowUtcPlus3Fallback",
            TimeSpan.FromHours(3),
            "Москва (UTC+3)",
            "Москва (UTC+3)");
    }
}
