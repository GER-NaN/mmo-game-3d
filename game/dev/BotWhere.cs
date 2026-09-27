namespace MmoGame3d.Dev;

using MmoGame3d.Rules.World;

/// <summary>Where a bot is, in the words activities use to say where they can start.</summary>
public static class BotWhere
{
    // In a zone, and not one made for a single use (a taxi cabin).
    public static bool InWorld(BotBody body)
    {
        return body.Zone != null && !ZoneIds.IsInstance(body.ZoneId);
    }

    public static bool InTown(BotBody body)
    {
        return body.ZoneId == ZoneIds.Town;
    }
}
