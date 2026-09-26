namespace MmoGame3d.Chests;

using Godot;
using MmoGame3d.Interact;

/// <summary>
/// A container standing in a zone that holds one thing at a time: open it to take what
/// is in it, and it fills again after a while. Whether it holds something is synced, so
/// everyone sees it full or empty; what it holds is decided on the server when it is
/// opened (ServerChests).
/// </summary>
public partial class Chest : Interactable
{
    [Export]
    public string ChestName { get; set; } = "old hardware chest";

    [Export]
    public bool HasItem { get; set; } = true;

    public override string Prompt
    {
        get { return HasItem ? "Open the " + ChestName : "The " + ChestName + " is empty"; }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        GetNode<Node3D>("Full").Visible = HasItem;
        GetNode<Node3D>("Empty").Visible = !HasItem;
    }
}
