namespace MmoGame3d.Rules.Social;

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
