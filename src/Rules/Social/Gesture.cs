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
