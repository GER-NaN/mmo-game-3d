namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// The activities only some personas do (BotPersonas): poking at every screen, running
/// for the edge of the world, squeezing into gaps.
/// </summary>
public static class BotExtraActivities
{
    public static readonly BotActivity PokeAround = new StepsActivity("poke around", 8, body => body.Zone != null && !body.ZoneId.StartsWith("taxi"), body => new List<BotStep>
    {
        new PokeStep(false, 25 + (body.Random.NextDouble() * 20)),
        new CloseAllStep(),
    });

    public static readonly BotActivity ShadowSomeone = new StepsActivity("shadow someone", 8, body => body.Zone != null && !body.ZoneId.StartsWith("taxi"), body => new List<BotStep>
    {
        new ShadowStep(40 + (body.Random.NextDouble() * 40)),
        new CloseAllStep(),
    });

    // One of the typed lines, in public chat, for everyone's chat box and the server.
    public static readonly BotActivity SaySomethingOdd = new StepsActivity("say something odd", 2, body => body.Zone != null, body =>
        new BotPlan()
            .Say(PokeStep.Lines[body.Random.Next(PokeStep.Lines.Length)])
            .Pause(1.5)
            .Steps);

    public static readonly BotActivity PokeAtTerminal = new StepsActivity("poke at a terminal", 5, body => body.ZoneId == ZoneIds.Town, body =>
    {
        string terminal = body.Random.Next(2) == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
        return new List<BotStep>
        {
            new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
            new UseStep("Go Online", b => b.IsOnline),
            new PokeStep(true, 25 + (body.Random.NextDouble() * 20)),
            new CloseAllStep(),
        };
    });

    public static readonly BotActivity RunForTheEdge = new StepsActivity("run for the edge", 8, body => body.Zone != null && !body.ZoneId.StartsWith("taxi"), body => new List<BotStep>
    {
        new EdgeStep(45),
    });

    public static readonly BotActivity MashKeys = new StepsActivity("mash the keys", 10, body => body.Zone != null, body => new List<BotStep>
    {
        new MashStep(10 + (body.Random.NextDouble() * 15)),
        new CloseAllStep(),
    });

    public static readonly BotActivity SqueezeIntoAGap = new StepsActivity("squeeze into a gap", 8, body => body.Zone?.GetNodeOrNull("Buildings") != null, body => new List<BotStep>
    {
        new SqueezeStep(),
    });
}





