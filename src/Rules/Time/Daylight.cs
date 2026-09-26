namespace MmoGame3d.Rules.Time;

/// <summary>
/// Where the sun is and how strong, from the hour. A simple day: the sun rises at six,
/// is highest at noon and sets at six. The numbers are placeholders until seen in the
/// game; the shape (dark night, soft dawn, bright noon) is the point.
/// </summary>
public static class Daylight
{
    public const double Sunrise = 6;
    public const double Sunset = 18;

    // The sun's height over the horizon as a fraction: 1 at noon, 0 at sunrise and
    // sunset, below 0 at night (-1 at midnight).
    public static double SunHeight(double hour)
    {
        return Math.Sin((hour - Sunrise) / 24.0 * 2.0 * Math.PI);
    }

    // How bright the sun is: 0 at night, rising fast after dawn.
    public static double SunStrength(double hour)
    {
        double height = SunHeight(hour);
        return height <= 0 ? 0 : Math.Min(1.0, height * 2.0);
    }

    // How warm the light is: 1 near the horizon (orange), 0 high in the sky (white).
    public static double Warmth(double hour)
    {
        double height = SunHeight(hour);
        return height <= 0 ? 1 : Math.Max(0.0, 1.0 - (height * 3.0));
    }
}
