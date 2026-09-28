namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Rules.World;

/// <summary>
/// The outskirts: a wander, the old hardware chest opened, and a plant on display
/// inspected, when there is one.
/// </summary>
public sealed class OutskirtsActivity : StepsActivity
{
    public OutskirtsActivity()
        : base("go to the outskirts", 2)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.Outskirts; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        List<BotStep> steps = new List<BotStep>
        {
            new WanderStep(6 + (body.Random.NextDouble() * 8)),
            new WalkToStep("walk to the chest", b => b.Thing("Interactables/OldHardwareChest")),
            new UseStep("Open the", b => !b.Prompt.Contains("Open the"), 4),
        };

        DisplayPlant? plant = AnyPlant(body);

        if (plant != null)
        {
            // By its spot, looked up each time: a new plant takes the spot, and the old
            // node is freed.
            string spot = Interact.Interactable.ParentName + "/" + plant.Name;
            steps.Add(new WalkToStep("walk to a plant on display", b => b.Zone?.GetNodeOrNull<DisplayPlant>(spot)));
            steps.Add(new UseStep("Inspect", b => false, 4, false, true));
            steps.Add(new PauseStep(2));
            steps.Add(new CloseAllStep());
        }

        return steps;
    }

    private static DisplayPlant? AnyPlant(BotBody body)
    {
        Node? things = body.Zone?.GetNodeOrNull("Interactables");
        List<DisplayPlant> plants = new List<DisplayPlant>();

        foreach (Node node in things?.GetChildren() ?? new Godot.Collections.Array<Node>())
        {
            DisplayPlant? plant = node as DisplayPlant;

            if (plant != null)
            {
                plants.Add(plant);
            }
        }

        return plants.Count == 0 ? null : plants[body.Random.Next(plants.Count)];
    }
}
