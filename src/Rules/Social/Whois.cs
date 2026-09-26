namespace MmoGame3d.Rules.Social;

using MmoGame3d.Rules.World;

/// <summary>
/// Whois, a player's page in the terminal (docs/features/social-page-concept.md): who
/// they are, their career, where they are and what they are doing, their Plan, their
/// skills if they show them, and props. What the owner sets is a WhoisSettings; the
/// rest is read from the game.
/// </summary>
public class WhoisSettings
{
    public const int MaxPlanLength = 140;

    public string Plan { get; set; } = "";
    public bool ShowSkills { get; set; }
    public bool ShowLocation { get; set; } = true;
}

public static class Whois
{
    public const int MaxResults = 20;
    public const int MinSearchLength = 2;

    // Where a player is, as the page says it, or "" when it goes dark: the owner turned
    // location off, or the player is away from a town (the author: "it can go dark when
    // the player is not near town").
    public static string Where(string zoneId, bool showLocation)
    {
        if (!showLocation)
        {
            return "";
        }

        switch (ZoneIds.SceneOf(zoneId))
        {
            case ZoneIds.Town:
                return "In town";
            case ZoneIds.Shop:
                return "In the electronics shop";
            case ZoneIds.College:
                return "At the college";
            default:
                return "";
        }
    }

    // "Offline, last seen 3 days ago".
    public static string LastSeen(DateTime savedAtUtc, DateTime nowUtc)
    {
        TimeSpan ago = nowUtc - savedAtUtc;

        if (ago.TotalMinutes < 2)
        {
            return "Offline, last seen just now";
        }

        if (ago.TotalHours < 1)
        {
            return "Offline, last seen " + (int)ago.TotalMinutes + " minutes ago";
        }

        if (ago.TotalDays < 1)
        {
            int hours = (int)ago.TotalHours;
            return "Offline, last seen " + hours + (hours == 1 ? " hour ago" : " hours ago");
        }

        int days = (int)ago.TotalDays;
        return "Offline, last seen " + days + (days == 1 ? " day ago" : " days ago");
    }
}
