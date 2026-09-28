namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;

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
