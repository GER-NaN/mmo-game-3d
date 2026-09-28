namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;

/// <summary>
/// Activities one after another, for orders a random pick would almost never reach. Next
/// is asked after each activity for the one after it, with how the last one ended; a
/// failed one does not end the chain, since the next must cope with what it left.
/// </summary>
public abstract class BotChain
{
    protected BotChain(string name, int weight, ChainKind kind)
    {
        Name = name;
        Weight = weight;
        Kind = kind;
    }

    public string Name { get; }

    public int Weight { get; }

    public ChainKind Kind { get; }

    // The bot's own business: out of any party first, and invites turned down.
    public virtual bool Solo
    {
        get { return false; }
    }

    // About how long it takes: a random cancel falls somewhere inside it.
    public virtual double UsualSeconds
    {
        get { return 180; }
    }

    // How it went so far, for the log and findings.
    public List<string> Done { get; } = new List<string>();

    // Why it ended early; "" when it ran out or met its goal.
    public string Why { get; protected set; } = "";

    public virtual bool CanStart(BotBody body)
    {
        return true;
    }

    public virtual void Begin(BotBody body, Random random)
    {
        Done.Clear();
        Why = "";
    }

    // The next activity, or null when the chain is over.
    public BotActivity? Next(BotBody body, BotActivity? last, BotEnd lastEnd)
    {
        if (last != null)
        {
            Done.Add(last.Name + (lastEnd == BotEnd.Finished ? "" : " (" + lastEnd.ToString().ToLowerInvariant() + ")"));
        }

        return NextAfter(body, last, lastEnd);
    }

    protected abstract BotActivity? NextAfter(BotBody body, BotActivity? last, BotEnd lastEnd);

    public string Describe()
    {
        return Name + ": " + (Done.Count == 0 ? "(nothing yet)" : string.Join(" > ", Done));
    }
}
