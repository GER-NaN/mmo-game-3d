namespace MmoGame3d.Overseer;

using System;

/// <summary>
/// One bot to start: an activity it plays once, or a persona it plays until stopped, and
/// which player it plays as. Written on the command line as "activity" or "@persona",
/// then ":connect" (a kept player of its own) or ":fresh" (a new player every launch).
/// </summary>
public class BotSpec
{
    public BotSpec(string activity, string persona, BotPlayer player)
    {
        Activity = activity;
        Persona = persona;
        Player = player;
    }

    // Empty for a persona bot.
    public string Activity { get; }

    // Empty for a one-activity bot.
    public string Persona { get; }

    public BotPlayer Player { get; }

    public bool IsPersona
    {
        get { return Persona.Length > 0; }
    }

    // The activity's or the persona's name, for the bot's own name.
    public string Name
    {
        get { return IsPersona ? Persona : Activity; }
    }

    public static BotSpec Parse(string text)
    {
        int colon = text.IndexOf(':');
        string what = colon < 0 ? text : text.Substring(0, colon);
        BotPlayer player = BotPlayer.None;

        if (colon >= 0)
        {
            string mode = text.Substring(colon + 1);

            switch (mode)
            {
                case "connect":
                    player = BotPlayer.Kept;
                    break;
                case "fresh":
                    player = BotPlayer.Fresh;
                    break;
                default:
                    throw new ArgumentException("Unknown player \"" + mode + "\" in \"" + text + "\": use connect or fresh.");
            }
        }

        return what.StartsWith("@") ? new BotSpec("", what.Substring(1), player) : new BotSpec(what, "", player);
    }
}
