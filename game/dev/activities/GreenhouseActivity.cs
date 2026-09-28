namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Gardening;
using MmoGame3d.Rules.World;

/// <summary>
/// Making a house plant at the greenhouse's potting table: how many pieces, which, where on
/// the soil and the plant's name are chosen afresh each time, so every run is a different
/// plant (GardenUi does the dragging).
/// </summary>
public sealed class GreenhouseActivity : StepsActivity
{
    private static readonly string[] Names = { "fern", "Spike", "office jungle", "Kevin", "the tall one", "Bot's Pride" };

    private int _pieces;

    public GreenhouseActivity()
        : base("make a house plant", 1)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Greenhouse; }
    }

    public override double UsualSeconds
    {
        get { return 50; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        BotPlan plan = new BotPlan()
            .WalkTo("Interactables/PottingTable")
            .Use("house plant", b => GardenUi.IsOpen(b))
            .Pause(0.8);

        _pieces = 1 + body.Random.Next(PlantDesign.MaxPieces);

        for (int i = 0; i < _pieces; i++)
        {
            // Somewhere on the soil, within most of its radius.
            float x = (float)((body.Random.NextDouble() * 1.4) - 0.7);
            float z = (float)((body.Random.NextDouble() * 1.4) - 0.7);
            plan = plan.Step(GardenUi.Place(body.Random.Next(64), x, z)).Pause(0.3);
        }

        return plan
            .Step(GardenUi.Complete(Names[body.Random.Next(Names.Length)]))
            .Step(GardenUi.WaitDone())
            .Pause(1.5)
            .Close()
            .Steps;
    }

    public override BotActivityJudge? NewJudge()
    {
        return new GreenhouseJudge();
    }
}
