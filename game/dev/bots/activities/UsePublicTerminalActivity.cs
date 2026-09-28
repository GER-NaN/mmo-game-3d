namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;

/// <summary>A public terminal (the library's or the street kiosk), online, a few apps, offline.</summary>
public sealed class UsePublicTerminalActivity : StepsActivity
{
    public UsePublicTerminalActivity()
        : base("use a public terminal", 4)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Town; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        // Chosen once, so it does not swing between the two on the way.
        string terminal = body.Random.Next(2) == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
        return new List<BotStep>
        {
            new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
            TerminalUi.GoOnline(),
            TerminalUi.Browse(),
        };
    }
}
