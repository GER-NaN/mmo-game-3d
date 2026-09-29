namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;

/// <summary>
/// Who a bot is (bots.md R3): which activities it picks and how often, and how easily it
/// walks away from one half done. It picks the next activity whenever it is free. A
/// persona made for one activity (Only) plays it once and is finished.
/// </summary>
public class BotPersona
{
    private readonly List<string> _names = new List<string>();
    private readonly List<double> _weights = new List<double>();
    private readonly bool _once;
    private bool _played;

    public BotPersona(string name)
        : this(name, false)
    {
    }

    private BotPersona(string name, bool once)
    {
        Name = name;
        _once = once;
    }

    public string Name { get; }

    // The chance, at each step's end, of leaving the activity there (R1).
    public double WalkAwayChance { get; private set; }

    // The chance of joining a party it is invited to; otherwise it says no.
    public double JoinChance { get; private set; } = 0.6;

    // Plays this activity once.
    public static BotPersona Only(string activity)
    {
        BotPersona persona = new BotPersona(activity, true);
        persona._names.Add(activity);
        persona._weights.Add(1);
        return persona;
    }

    public BotPersona Likes(string activity, double weight)
    {
        _names.Add(activity);
        _weights.Add(weight);
        return this;
    }

    public BotPersona WalksAway(double chance)
    {
        WalkAwayChance = chance;
        return this;
    }

    public BotPersona JoinsInvites(double chance)
    {
        JoinChance = chance;
        return this;
    }

    // Its activities, for checking them against the registry.
    public IReadOnlyList<string> Activities
    {
        get { return _names; }
    }

    // The next activity's name, by weight; null when a one-activity persona has played.
    public string? Next(Random random)
    {
        if (_once && _played)
        {
            return null;
        }

        _played = true;
        double total = 0;

        foreach (double weight in _weights)
        {
            total += weight;
        }

        double pick = random.NextDouble() * total;

        for (int i = 0; i < _names.Count; i++)
        {
            pick -= _weights[i];

            if (pick <= 0)
            {
                return _names[i];
            }
        }

        return _names.Count > 0 ? _names[_names.Count - 1] : null;
    }
}
