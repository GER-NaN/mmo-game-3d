namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>The parts of the zone's map not discovered yet, walked to.</summary>
public sealed class ExploreActivity : StepsActivity
{
    public ExploreActivity()
        : base("explore this zone", 2)
    {
    }

    public override double UsualSeconds
    {
        get { return 120; }
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && body.Undiscovered().Count > 0;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new ExploreStep() };
    }
}
