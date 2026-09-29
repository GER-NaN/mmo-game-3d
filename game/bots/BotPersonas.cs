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
            .Likes("wander-town", 2)
            .Likes("jump", 1)
            .Likes("meadows", 1)
            .Likes("old-town-explorer", 1)
            .Likes("phone-terminal", 1)
            .Likes("look-at-map", 1)
            .Likes("talk-to-townsperson", 1)
            .Likes("emote-wave", 1)
            .Likes("pick-up", 1)
            .Likes("recycle", 1)
            .Likes("fix-something", 1)
            .Likes("ride-taxi", 1)
            .Likes("befriend-someone", 1)
            .Likes("invite-someone", 1)
            .Likes("message-someone", 1)
            .WalksAway(0.02),

        // Opens every screen and clicks what it finds.
        new BotPersona("curious")
            .Likes("poke-around", 3)
            .Likes("terminal-tour", 2)
            .Likes("look-at-bag", 1)
            .Likes("look-at-map", 1)
            .Likes("look-at-skills", 1)
            .Likes("look-at-friends", 1)
            .Likes("game-menu", 1)
            .Likes("game-settings", 1)
            .Likes("wardrobe", 1)
            .Likes("whois-mine", 1)
            .Likes("read-visitor-book", 1)
            .Likes("talk-to-registrar", 1)
            .Likes("talk-to-professor", 1)
            .Likes("give-someone-something", 1)
            .Likes("leave-party", 1)
            .WalksAway(0.05),

        // The minigames, over and over.
        new BotPersona("gamer")
            .Likes("code-cracker", 3)
            .Likes("agent-defense", 3)
            .Likes("house-plant", 2)
            .Likes("two-plants", 1),

        // Runs for the edges and the far places.
        new BotPersona("escaper")
            .Likes("run-for-the-edge", 4)
            .Likes("wooded-path-stroll", 1)
            .Likes("wander-new-town", 1)
            .Likes("meadows", 1)
            .Likes("squeeze-into-a-corner", 2),

        // All the money it can: picking up, recycling, drones, the chest, the cameras.
        new BotPersona("earner")
            .Likes("pick-up", 3)
            .Likes("recycle", 3)
            .Likes("open-chest", 1)
            .Likes("hunt-drone", 2)
            .Likes("surveillance-town", 1)
            .Likes("buy-recycle-buy", 1)
            .Likes("repair-lights", 1)
            .Likes("fix-something", 2),

        // Every door and every zone.
        new BotPersona("traveller")
            .Likes("travel-the-long-way", 2)
            .Likes("ping-pong-new-town", 1)
            .Likes("old-town-explorer", 2)
            .Likes("surveyor-town", 1)
            .Likes("surveyor-new_town", 1)
            .Likes("surveyor-wooded_path", 1)
            .Likes("surveyor-outskirts", 1)
            .Likes("tag-subway", 1)
            .Likes("wander-new-town", 1)
            .WalksAway(0.03),

        // Keys in any order, emotes, and walking away from half the things it starts.
        new BotPersona("masher")
            .Likes("mash-keys", 3)
            .Likes("emote-cheer", 1)
            .Likes("emote-sit", 1)
            .Likes("jump", 1)
            .Likes("wander-town", 1)
            .Likes("phone-day", 1)
            .WalksAway(0.2),

        // Loses its connection in the middle of things, and its characters change.
        new BotPersona("dropper")
            .Likes("drop-mid-walk", 2)
            .Likes("drop-at-terminal", 1)
            .Likes("leave-and-return", 1)
            .Likes("create-character", 1)
            .Likes("switch-character", 2)
            .Likes("wardrobe", 1)
            .Likes("wander-town", 2)
            .Likes("enroll", 1),
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
