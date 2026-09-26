namespace MmoGame3d.Players;

using Godot;

/// <summary>
/// The client's camera: behind its own player, turning with the heading and trailing
/// a little. Right-drag looks round and tilts; once the player walks, the look eases
/// back behind, but the tilt stays. The wheel zooms. The server has no player of its
/// own, so there it finds nothing to follow.
/// </summary>
public partial class ChaseCamera : Camera3D
{
    // The distance and tilt are placeholders until seen in the game.
    private const float StartDistance = 8f;
    private const float MinDistance = 3f;
    private const float MaxDistance = 25f;
    private const float ZoomStep = 1.12f;
    private const float StartPitch = -0.45f;
    private const float MinPitch = -1.35f;
    private const float MaxPitch = -0.05f;
    private const float DragRadiansPerPixel = 0.006f;

    // Per second: how fast the camera catches up with the heading, and how fast a look
    // round returns behind once walking.
    private const float FollowRate = 8f;
    private const float LookReturnRate = 3f;

    // The point looked at, above the feet.
    private static readonly Vector3 LookHeight = new Vector3(0f, 1.5f, 0f);

    private float _distance = StartDistance;
    private float _pitch = StartPitch;
    private float _lookYaw;
    private float _yaw;
    private bool _dragging;
    private bool _placed;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.Right:
                    _dragging = button.Pressed;
                    break;
                case MouseButton.WheelUp:
                    _distance = Mathf.Clamp(_distance / ZoomStep, MinDistance, MaxDistance);
                    break;
                case MouseButton.WheelDown:
                    _distance = Mathf.Clamp(_distance * ZoomStep, MinDistance, MaxDistance);
                    break;
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragging)
        {
            _lookYaw -= motion.Relative.X * DragRadiansPerPixel;
            _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * DragRadiansPerPixel), MinPitch, MaxPitch);
        }
    }

    public override void _Process(double delta)
    {
        Player? player = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;

        if (player == null)
        {
            _placed = false;
            return;
        }

        float step = (float)delta;

        if (player.IsWalking && !_dragging)
        {
            _lookYaw = Mathf.LerpAngle(_lookYaw, 0f, 1f - Mathf.Exp(-LookReturnRate * step));
        }

        float wanted = player.Heading + _lookYaw;
        _yaw = _placed ? Mathf.LerpAngle(_yaw, wanted, 1f - Mathf.Exp(-FollowRate * step)) : wanted;
        _placed = true;

        // Behind is +Z in the player's frame, since the player faces -Z. The pitch turns
        // that up and over, then the yaw turns it round the player.
        Basis orbit = new Basis(Vector3.Up, _yaw) * new Basis(Vector3.Right, _pitch);
        Vector3 target = player.GlobalPosition + LookHeight;

        Position = target + (orbit * new Vector3(0f, 0f, _distance));
        LookAt(target, Vector3.Up);
    }
}
