namespace MmoGame3d.Gardening;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Rules.Gardening;

/// <summary>
/// A player's house plant on display: it can be inspected, never picked up. What it is
/// travels once, at spawn; each client rebuilds it from the design.
/// </summary>
public partial class DisplayPlant : Interactable
{
    // The pots are life-size in the art; on display a plant is a little smaller.
    // Placeholder.
    private const float DisplayScale = 0.7f;

    [Export]
    public long PlantId { get; set; }

    [Export]
    public string PlantName { get; set; } = "";

    [Export]
    public string CreatorName { get; set; } = "";

    [Export]
    public string Design { get; set; } = "";

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    public string Title
    {
        get { return PlantName.Length > 0 ? PlantName : "House plant #" + PlantId; }
    }

    public override string Prompt
    {
        get { return "Inspect " + Title; }
    }

    public override void _Ready()
    {
        GetNode<Label3D>("Label").Text = Title + "\nby " + CreatorName;

        if (Multiplayer.IsServer())
        {
            return;
        }

        PlantDesign? design = PlantDesign.Parse(Design);

        if (design != null)
        {
            Node3D plant = PlantBuilder.Build(design);
            plant.Scale = Vector3.One * DisplayScale;
            plant.Position = new Vector3(0f, 0.3f, 0f);
            AddChild(plant);
        }
    }
}
