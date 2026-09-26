namespace MmoGame3d.Drones;

using Godot;

/// <summary>
/// A hostile-looking surveillance drone (world.md: the AI's eyes are dense in towns). The
/// server flies it in a slow circle around its pair's centre; an EMP pulse knocks it
/// down, it falls, lies a moment, and is gone. Clients draw it and smooth its synced
/// position. The look is a placeholder built from boxes and cylinders.
/// </summary>
public partial class Drone : Node3D
{
    public const float Height = 3.5f;

    // Placeholders.
    private const float CircleRadius = 3f;
    private const float CircleSpeed = 0.5f;
    private const float Gravity = 12f;
    private const float RotorSpin = 40f;
    private const float Smoothing = 10f;

    private double _time;
    private float _fallSpeed;

    // Where the pair circles, zone-local. Set by the server before spawning.
    [Export]
    public Vector3 Center { get; set; }

    // Where on the circle this one flies: the pair are half a turn apart.
    [Export]
    public float Phase { get; set; }

    [Export]
    public Vector3 NetPosition { get; set; }

    // Knocked out by an EMP: falling, then lying on the ground.
    [Export]
    public bool Down { get; set; }

    // Server only: seconds since it hit the ground, for despawning.
    public double LyingFor { get; private set; }

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    private AudioStreamPlayer3D? _hum;

    public override void _Ready()
    {
        Position = NetPosition;

        if (!Multiplayer.IsServer())
        {
            _hum = Audio.AudioDirector.Current?.Attach("fx.drone_hum", this);
        }
    }

    public override void _Process(double delta)
    {
        float step = (float)delta;

        if (Multiplayer.IsServer())
        {
            Fly(step);
            return;
        }

        Position = Position.Lerp(NetPosition, 1f - Mathf.Exp(-Smoothing * step));

        if (!Down)
        {
            foreach (string rotor in new[] { "Rotors/A", "Rotors/B", "Rotors/C", "Rotors/D" })
            {
                GetNode<Node3D>(rotor).RotateY(RotorSpin * step);
            }
        }

        GetNode<OmniLight3D>("Eye/Light").Visible = !Down;
        GetNode<Node3D>("Eye").Visible = !Down;

        // A downed drone goes quiet.
        if (Down && _hum != null)
        {
            _hum.QueueFree();
            _hum = null;
        }
    }

    private void Fly(float step)
    {
        _time += step;

        if (Down)
        {
            if (NetPosition.Y > 0.15f)
            {
                _fallSpeed += Gravity * step;
                NetPosition = new Vector3(NetPosition.X, Mathf.Max(0.15f, NetPosition.Y - (_fallSpeed * step)), NetPosition.Z);
            }
            else
            {
                LyingFor += step;
            }

            Position = NetPosition;
            return;
        }

        float angle = (float)(_time * CircleSpeed) + Phase;
        float bob = Mathf.Sin((float)_time * 1.7f + Phase) * 0.25f;
        NetPosition = Center + new Vector3(Mathf.Cos(angle) * CircleRadius, Height + bob, Mathf.Sin(angle) * CircleRadius);
        Position = NetPosition;
        Rotation = new Vector3(0f, -angle, 0f);
    }
}
