using System;

namespace WebServer.Utilities;

/// <summary>
/// Centralizes conversions between UTC and the municipality's timezone (Asia/Jerusalem).
/// </summary>
public static class IsraelTime
{
    private static readonly TimeZoneInfo IsraelZone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        try   
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    public static DateTimeOffset NowUtc => DateTimeOffset.UtcNow;

    public static DateTimeOffset Convert(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, IsraelZone);

    public static DateTime? Convert(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var date = value.Value;
        if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
        else
        {
            date = date.ToUniversalTime();
        }

        return TimeZoneInfo.ConvertTimeFromUtc(date, IsraelZone);
    }
}
