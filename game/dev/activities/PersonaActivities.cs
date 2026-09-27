namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;

/// <summary>
/// The activities only some personas do (BotPersonas, Own): poking at every screen,
/// running for the edge of the world, squeezing into gaps, mashing keys, shadowing
/// someone, saying odd things.
/// </summary>
public static class PersonaActivities
{
    public static readonly BotActivity PokeAround = new PokeAroundActivity();
    public static readonly BotActivity PokeAtTerminal = new PokeAtTerminalActivity();
    public static readonly BotActivity RunForTheEdge = new RunForTheEdgeActivity();
    public static readonly BotActivity MashKeys = new MashKeysActivity();
    public static readonly BotActivity SqueezeIntoAGap = new SqueezeIntoAGapActivity();
    public static readonly BotActivity ShadowSomeone = new ShadowSomeoneActivity();
    public static readonly BotActivity SaySomethingOdd = new SaySomethingOddActivity();
}

/// <summary>Whatever is open, poked at: buttons clicked, fields typed into, panels opened.</summary>
public sealed class PokeAroundActivity : StepsActivity
{
    public PokeAroundActivity()
        : base("poke around", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new PokeStep(false, 25 + (body.Random.NextDouble() * 20)), new CloseAllStep() };
    }
}

/// <summary>A public terminal's screen, poked at.</summary>
public sealed class PokeAtTerminalActivity : StepsActivity
{
    public PokeAtTerminalActivity()
        : base("poke at a terminal", 5)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        string terminal = body.Random.Next(2) == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
        return new List<BotStep>
        {
            new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
            TerminalUi.GoOnline(),
            new PokeStep(true, 25 + (body.Random.NextDouble() * 20)),
            new CloseAllStep(),
        };
    }
}

/// <summary>Straight for a point past the zone's edge, jumping: the ways out of the world.</summary>
public sealed class RunForTheEdgeActivity : StepsActivity
{
    public RunForTheEdgeActivity()
        : base("run for the edge", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new EdgeStep(45) };
    }
}

/// <summary>The game's keys, fast and in any order.</summary>
public sealed class MashKeysActivity : StepsActivity
{
    public MashKeysActivity()
        : base("mash the keys", 10)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new MashStep(10 + (body.Random.NextDouble() * 15)), new CloseAllStep() };
    }
}

/// <summary>Into the gap between two buildings, pushing: where players get wedged.</summary>
public sealed class SqueezeIntoAGapActivity : StepsActivity
{
    public SqueezeIntoAGapActivity()
        : base("squeeze into a gap", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone?.GetNodeOrNull("Buildings") != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new SqueezeStep() };
    }
}

/// <summary>Another player followed at arm's length, through doors, using what they use.</summary>
public sealed class ShadowSomeoneActivity : StepsActivity
{
    public ShadowSomeoneActivity()
        : base("shadow someone", 8)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new ShadowStep(40 + (body.Random.NextDouble() * 40)), new CloseAllStep() };
    }
}

/// <summary>One of the hostile lines (markup, emoji, too long), in public chat.</summary>
public sealed class SaySomethingOddActivity : StepsActivity
{
    public SaySomethingOddActivity()
        : base("say something odd", 2)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Zone != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { ChatUi.Say(PokeStep.Lines[body.Random.Next(PokeStep.Lines.Length)]), new PauseStep(1.5) };
    }
}
