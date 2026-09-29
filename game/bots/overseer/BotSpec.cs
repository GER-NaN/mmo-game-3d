namespace MmoGame3d.Overseer;

using System;

/// <summary>
/// One bot to start: the activity it runs and which player it plays as. Written on the
/// command line as "activity", "activity:connect" (a kept player of its own) or
/// "activity:fresh" (a new player every launch).
/// </summary>
public class BotSpec
{
    public BotSpec(string activity, BotPlayer player)
    {
        Activity = activity;
        Player = player;
    }

    public string Activity { get; }

    public BotPlayer Player { get; }

    public static BotSpec Parse(string text)
    {
        int colon = text.IndexOf(':');

        if (colon < 0)
        {
            return new BotSpec(text, BotPlayer.None);
        }

        string mode = text.Substring(colon + 1);
        string activity = text.Substring(0, colon);

        switch (mode)
        {
            case "connect":
                return new BotSpec(activity, BotPlayer.Kept);
            case "fresh":
                return new BotSpec(activity, BotPlayer.Fresh);
            default:
                throw new ArgumentException("Unknown player \"" + mode + "\" in \"" + text + "\": use connect or fresh.");
        }
    }
}
