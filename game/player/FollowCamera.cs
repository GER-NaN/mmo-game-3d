namespace MmoGame3d.Players;

using Godot;

// Follows this client's own player from above and behind. The server has no player of
// its own, so there it finds nothing to follow.
public partial class FollowCamera : Camera3D
{
    private static readonly Vector3 Offset = new Vector3(0f, 8f, 8f);

    public override void _Process(double delta)
    {
        Node3D? player = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Node3D;

        if (player == null)
        {
            return;
        }

        Position = player.GlobalPosition + Offset;
        LookAt(player.GlobalPosition + Vector3.Up, Vector3.Up);
    }
}
