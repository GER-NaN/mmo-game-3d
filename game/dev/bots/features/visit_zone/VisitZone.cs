namespace MmoGame3d.Dev.Features;

using System.Collections.Generic;
using MmoGame3d.Rules.World;

/// <summary>
/// A visit to another zone, picked at random, and a look round once there. Every zone in
/// ZoneIds.All is visited, so a new zone needs no change here.
/// </summary>
public sealed class VisitZone : BotFeature
{
    public VisitZone()
        : base("visit a zone", 3)
    {
    }

    protected override BotPlan Steps(BotBody body)
    {
        List<string> others = new List<string>(ZoneIds.All);
        others.Remove(body.ZoneId);
        string zone = others[body.Random.Next(others.Count)];

        return new BotPlan()
            .Travel(zone)
            .Wander(10 + (body.Random.NextDouble() * 10));
    }
}
