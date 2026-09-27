namespace MmoGame3d.Rules.Social;

// What a body is doing with itself for a moment: an emote a player asked for, or an
// action the server shows (picking something up, repairing). One at a time; walking
// ends it.
public class Gesture
{
    public Gesture(string id, string animation, double seconds, bool isEmote)
        : this(id, animation, seconds, isEmote, "")
    {
    }

    public Gesture(string id, string animation, double seconds, bool isEmote, string tool)
    {
        Id = id;
        Animation = animation;
        Seconds = seconds;
        IsEmote = isEmote;
        Tool = tool;
    }

    public string Id { get; }

    // The rig animation it plays.
    public string Animation { get; }

    // How long it lasts; 0 for until the player moves.
    public double Seconds { get; }

    // True for what a player may ask for in chat; false for what only the server shows.
    public bool IsEmote { get; }

    // What is in the right hand while it plays: a model in the KayKit tools pack, by
    // name ("hammer"), or empty for nothing.
    public string Tool { get; }
}

/// <summary>
/// The gestures there are. Emotes are chat commands ("/wave"); the rest the server
/// plays for actions. Both sides read this list, so only the id travels.
/// </summary>
public static class Gestures
{
    public const string PickUp = "pickup";
    public const string Repair = "repair";
    public const string Work = "work";
    public const string Tinker = "tinker";

    private static readonly Gesture[] All =
    {
        new Gesture("wave", "Waving", 2.5, true),
        new Gesture("cheer", "Cheering", 3, true),
        new Gesture("sit", "Sit_Floor_Idle", 0, true),
        new Gesture("pushups", "Push_Ups", 0, true),
        new Gesture(PickUp, "PickUp", 1, false),
        new Gesture(Repair, "Hammering", 2, false, "hammer"),
        new Gesture(Work, "Working_A", 2, false),
        new Gesture(Tinker, "Working_A", 2, false, "screwdriver_A_short_color"),
    };

    public static Gesture? Find(string id)
    {
        foreach (Gesture gesture in All)
        {
            if (gesture.Id == id)
            {
                return gesture;
            }
        }

        return null;
    }

    // "/wave" and the like, to the emote id; null when the line is not an emote.
    public static string? EmoteIn(string chatLine)
    {
        string text = chatLine.Trim().ToLowerInvariant();

        if (!text.StartsWith('/'))
        {
            return null;
        }

        Gesture? gesture = Find(text.Substring(1));
        return gesture != null && gesture.IsEmote ? gesture.Id : null;
    }

    public static IEnumerable<string> EmoteCommands()
    {
        foreach (Gesture gesture in All)
        {
            if (gesture.IsEmote)
            {
                yield return "/" + gesture.Id;
            }
        }
    }
}
