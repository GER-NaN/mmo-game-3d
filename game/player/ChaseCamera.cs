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
    public const float MinDistance = 3f;
    public const float MaxDistance = 25f;
    private const float StartDistance = 8f;
    private const float ZoomStep = 1.12f;
    private const float StartPitch = -0.45f;
    private const float MinPitch = -1.35f;
    private const float MaxPitch = -0.05f;
    private const float DragRadiansPerPixel = 0.006f;

    // A controller, per second at full tilt: radians of camera tilt, and how fast the
    // triggers zoom (e-fold per second). Placeholders until felt.
    private const float PadTiltRate = 1.5f;
    private const float PadZoomRate = 1.2f;

    // Per second: how fast the camera catches up with the heading, and how fast a look
    // round returns behind once walking.
    private const float FollowRate = 8f;
    private const float LookReturnRate = 3f;

    // How far in front of a wall the camera stops, so it does not see through it; and
    // how fast it moves back out, a second, once the wall is behind it.
    private const float WallGap = 0.3f;
    private const float EaseOutRate = 4f;

    // The point looked at, above the feet.
    private static readonly Vector3 LookHeight = new Vector3(0f, 1.5f, 0f);

    private float _distance = StartDistance;
    private float _shownDistance = StartDistance;
    private float _pitch = StartPitch;
    private float _lookYaw;
    private float _yaw;
    private bool _dragging;
    private bool _placed;

    // Set from the player's settings.
    public float Sensitivity { get; set; } = 1f;

    // Told when the wheel changes the distance, so it is kept for next time.
    public event System.Action<float>? Zoomed;

    public float Distance
    {
        get
        {
            return _distance;
        }

        set
        {
            _distance = Mathf.Clamp(value, MinDistance, MaxDistance);
        }
    }

    // After the bodies, so the camera follows where the player is this frame, not where
    // they were last frame. Bodies run at the default priority, 0.
    public const string ScreenGroup = "full_screen";

    public override void _Ready()
    {
        ProcessPriority = 100;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // A screen over the whole view (the terminal, the potting table) has the mouse;
        // its wheel and drags are not the world camera's.
        if (GetTree().GetNodeCountInGroup(ScreenGroup) > 0)
        {
            _dragging = false;
            return;
        }

        if (@event is InputEventMouseButton button)
        {
            switch (button.ButtonIndex)
            {
                case MouseButton.Right:
                    _dragging = button.Pressed;
                    break;
                case MouseButton.WheelUp:
                    Distance = _distance / ZoomStep;
                    Zoomed?.Invoke(_distance);
                    break;
                case MouseButton.WheelDown:
                    Distance = _distance * ZoomStep;
                    Zoomed?.Invoke(_distance);
                    break;
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragging)
        {
            float turn = DragRadiansPerPixel * Sensitivity;
            _lookYaw -= motion.Relative.X * turn;
            _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * turn), MinPitch, MaxPitch);
        }
    }

    // A controller's right stick up and down tilts, as a drag does; its triggers zoom.
    // The stick's left and right turn the player (Player.ReadInput), not the camera.
    private void ReadPad(float step)
    {
        if (GetTree().GetNodeCountInGroup(ScreenGroup) > 0)
        {
            return;
        }

        float tilt = Input.GetAxis("look_down", "look_up");
        _pitch = Mathf.Clamp(_pitch + (tilt * PadTiltRate * Sensitivity * step), MinPitch, MaxPitch);
        float zoom = Input.GetAxis("zoom_out", "zoom_in");

        if (zoom != 0f)
        {
            Distance = _distance * Mathf.Exp(-zoom * PadZoomRate * step);
            Zoomed?.Invoke(_distance);
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
        ReadPad(step);

        if (player.IsWalking && !_dragging)
        {
            _lookYaw = Mathf.LerpAngle(_lookYaw, 0f, 1f - Mathf.Exp(-LookReturnRate * step));
        }

        float wantedYaw = player.Heading + _lookYaw;
        _yaw = _placed ? Mathf.LerpAngle(_yaw, wantedYaw, 1f - Mathf.Exp(-FollowRate * step)) : wantedYaw;
        _placed = true;

        // Behind is +Z in the player's frame, since the player faces -Z. The pitch turns
        // that up and over, then the yaw turns it round the player.
        Basis orbit = new Basis(Vector3.Up, _yaw) * new Basis(Vector3.Right, _pitch);
        Vector3 target = player.GlobalPosition + LookHeight;

        // In at once when a wall is in the way, so it never looks through; back out gently,
        // so passing a corner does not throw the view about.
        Vector3 back = orbit * new Vector3(0f, 0f, 1f);
        float allowed = Unblocked(target, back, _distance);
        _shownDistance = allowed < _shownDistance ? allowed : Mathf.Lerp(_shownDistance, allowed, 1f - Mathf.Exp(-EaseOutRate * step));

        Position = target + (back * _shownDistance);
        LookAt(target, Vector3.Up);
    }

    // How far back along the view the camera may be: its distance, or less where a
    // building or a wall is in the way. Only CameraBlock things count; thin ones (trunks,
    // posts) would make it snap in and out as it passed them.
    private float Unblocked(Vector3 target, Vector3 back, float distance)
    {
        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(target, target + (back * distance), PhysicsLayers.CameraBlock);
        Godot.Collections.Dictionary hit = GetWorld3D().DirectSpaceState.IntersectRay(query);

        if (hit.Count == 0)
        {
            return distance;
        }

        Vector3 point = (Vector3)hit["position"];
        return Mathf.Max(0.5f, point.DistanceTo(target) - WallGap);
    }
}
