namespace MmoGame3d.Interact;

using Godot;

/// <summary>
/// Something in a zone you walk up to and use: a terminal, and later a vendor, a
/// workbench, a chest. It lives under the zone's "Interactables" node, so client and
/// server find it by its name. The node holds only what both sides show; what using it
/// does is decided on the server (ServerInteractions), which also checks reach.
/// </summary>
public abstract partial class Interactable : StaticBody3D
{
    public const string ParentName = "Interactables";

    // How close the player's feet must be, in world units, measured flat.
    [Export]
    public float Reach { get; set; } = 2.5f;

    // What the prompt says on the client ("Go Online: Public terminal"), or empty when
    // there is nothing to do here.
    public abstract string Prompt { get; }

    public MultiplayerSynchronizer? Synchronizer
    {
        get { return GetNodeOrNull<MultiplayerSynchronizer>("Synchronizer"); }
    }

    // feet is global. The server passes some slack, for a body that moved on by the time
    // its request arrived.
    public bool IsInReach(Vector3 feet, float slack = 0f)
    {
        Vector3 here = GlobalPosition;
        return new Vector2(here.X - feet.X, here.Z - feet.Z).Length() <= Reach + slack;
    }
}
