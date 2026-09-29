namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// The personas (bots.md R3), by name, written by hand: who a soak bot is, as weights over
/// the registry's activities and how readily it walks away. bot.json names one.
/// </summary>
public static class BotPersonas
{
    public static readonly List<BotPersona> All = new List<BotPersona>
    {
        // A bit of everything a player does in the world.
        new BotPersona("wanderer")
            .Likes("jump", 1)
            .Likes("meadows", 1)
            .Likes("old-town-explorer", 1)
            .Likes("phone-terminal", 1)
            .Likes("surveyor-town", 0.5)
            .Likes("surveyor-new_town", 0.5)
            .WalksAway(0.02),
    };

    public static BotPersona? Named(string name)
    {
        foreach (BotPersona persona in All)
        {
            if (persona.Name == name)
            {
                return persona;
            }
        }

        return null;
    }
}
