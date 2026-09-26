namespace MmoGame3d.Zones;

using System;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// A way from one zone to another: walk into it and you arrive in the target zone at a
/// named marker under its "Arrivals". The server notices the touch; a client only draws
/// the threshold, a faint glowing sheet across the way.
/// </summary>
public partial class Door : Area3D
{
    [Export]
    public string TargetZone { get; set; } = "";

    [Export]
    public string TargetArrival { get; set; } = "";

    // Raised on the server when a player walks in.
    public event Action<Door, Player>? Entered;

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            BodyEntered += OnBodyEntered;
            GetNodeOrNull<Node3D>("Threshold")?.QueueFree();
        }
        else
        {
            Monitoring = false;
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        Player? player = body as Player;

        if (player != null)
        {
            Entered?.Invoke(this, player);
        }
    }
}
