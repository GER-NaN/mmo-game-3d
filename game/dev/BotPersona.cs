namespace MmoGame3d.Dev;

using System.Collections.Generic;

/// <summary>
/// Who a bot is (--persona): numbers the driver reads, no code of its own. The activities
/// and chains are the same for everyone; a persona weighs them (a factor on each one's own
/// weight, 0 to leave it out), shares its free moments between activities, chains and
/// asides, adds activities of its own, and sets its pace, how often it cancels what it is
/// doing, how often its connection drops and how readily it joins a party. Several side by
/// side play the game several ways at once.
/// </summary>
public sealed class BotPersona
{
    public BotPersona(string name, string about)
    {
        Name = name;
        About = about;
    }

    public string Name { get; }

    // One line, said in chat when the bot arrives, and in the docs.
    public string About { get; }

    // Everything it waits (pauses, reading, lingering between activities) is this many
    // times as long. 1 is the wanderer's pace.
    public double Pace { get; set; } = 1;

    // When free: how often it takes an activity, a chain or an aside, against each other.
    public double ActivityShare { get; set; } = 60;

    public double ChainShare { get; set; } = 30;

    public double AsideShare { get; set; } = 10;

    // Asides also fire on their own timers; this many times as often (2) or as seldom (0.5).
    public double AsideRate { get; set; } = 1;

    // The chance, per activity or chain it takes, that it walks away from it at a random
    // moment, leaving everything as it is (BotDriver's interrupt roller).
    public double CancelChance { get; set; } = 0.25;

    public double JoinChance { get; set; } = 0.35;

    // The chance, per activity, that the connection drops somewhere in it: the state a
    // lost connection leaves (in a taxi, online, mid-game), and the login back into it.
    public double CutChance { get; set; }

    // Factors on activity and goal weights, by name; a name not here keeps factor 1.
    public Dictionary<string, double> Likes { get; } = new Dictionary<string, double>();

    // Activities only this persona does, with their own weights.
    public List<BotActivity> Own { get; } = new List<BotActivity>();

    public double Factor(string name)
    {
        double factor;
        return Likes.TryGetValue(name, out factor) ? factor : 1;
    }
}
