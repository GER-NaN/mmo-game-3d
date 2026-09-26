namespace MmoGame3d.Rules.Time;

/// <summary>
/// The world runs on a real clock: a server is a place, and its day follows the local
/// time where it stands. This one keeps Eastern time; a server for Korea would keep
/// Korean time. A dev offset shifts the hour, so noon can be tested at midnight.
/// </summary>
public class WorldClock
{
    public const string DefaultTimeZone = "America/New_York";
    public const double SecondsPerDay = 24 * 60 * 60;

    private readonly TimeZoneInfo _zone;
    private readonly double _offsetHours;

    public WorldClock(string timeZoneId, double offsetHours)
    {
        // IANA or Windows ids both work on .NET 6 and later.
        _zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        _offsetHours = offsetHours;
    }

    // Seconds since local midnight, 0 up to a day.
    public double SecondsOfDay(DateTime utcNow)
    {
        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), _zone);
        double seconds = local.TimeOfDay.TotalSeconds + (_offsetHours * 3600);
        double wrapped = seconds % SecondsPerDay;
        return wrapped < 0 ? wrapped + SecondsPerDay : wrapped;
    }
}
