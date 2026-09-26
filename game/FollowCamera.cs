namespace MmoGame3d;

using Godot;

// Follows this client's own player from above and behind. On the server it does nothing.
public partial class FollowCamera : Camera3D
{
    private static readonly Vector3 Offset = new Vector3(0f, 8f, 8f);

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        Node3D? player = GetNodeOrNull<Node3D>("../Players/" + Multiplayer.GetUniqueId());

        if (player == null)
        {
            return;
        }

        Position = player.Position + Offset;
        LookAt(player.Position + Vector3.Up, Vector3.Up);
    }
}
