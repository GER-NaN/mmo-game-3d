namespace MmoGame3d.Vendors;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Players;

/// <summary>
/// A shopkeeper: use them to see what their shop sells. What a shop sells and for how
/// much is in the rules (Shops), the same on both sides; the server does the selling.
/// </summary>
public partial class Vendor : Interactable
{
    private const string KeeperModel = "res://assets/kaykit/characters/Protagonist_B.glb";

    [Export]
    public string ShopId { get; set; } = "";

    [Export]
    public string KeeperName { get; set; } = "Shopkeeper";

    public override string Prompt
    {
        get { return "Talk to " + KeeperName; }
    }

    public override void _Ready()
    {
        GetNode<Label3D>("NameLabel").Text = KeeperName;

        if (!Multiplayer.IsServer())
        {
            AddChild(new CharacterModel { Name = "Model", ModelPath = KeeperModel });
        }
    }
}
