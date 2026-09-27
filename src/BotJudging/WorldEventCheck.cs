namespace MmoGame3d.BotJudging;

using System.Collections.Generic;

/// <summary>
/// A world event a bot went to, judged from its phone: the Notifications app's past rows,
/// newest first, read after the event's drones were gone. A bot that was in the event's
/// zone while its drones flew took part, so the newest past row for the event must say so.
/// </summary>
public static class WorldEventCheck
{
    public const string TookPart = "you took part";

    public static string? Judge(string line, bool sawItRunning, bool readAfter, IReadOnlyList<string> pastAfter)
    {
        if (!sawItRunning || !readAfter)
        {
            return null;
        }

        foreach (string row in pastAfter)
        {
            if (row.StartsWith(line))
            {
                return row.Contains(TookPart) ? null : "was in the zone while \"" + line + "\" ran, and Notifications does not say it took part: " + row;
            }
        }

        return "was in the zone while \"" + line + "\" ran, and Notifications shows no past row for it";
    }
}
