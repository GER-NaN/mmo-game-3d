namespace MmoGame3d.Town;

using Godot;
using MmoGame3d.Interact;

/// <summary>
/// A small thing in town that breaks now and then (a hydrant, a bench): the Field repair
/// skill's act. The prop is a child; this adds the broken state, synced so everyone sees
/// the sparks, and the prompt. The server breaks them (ServerFixables).
/// </summary>
public partial class Fixable : Interactable
{
    [Export]
    public string FixableName { get; set; } = "fire hydrant";

    [Export]
    public bool Broken { get; set; }

    public override string Prompt
    {
        get { return Broken ? "Fix the " + FixableName : ""; }
    }

    // How far a broken one leans, in degrees: a placeholder look.
    private const float BrokenLean = 7f;

    private double _flicker;
    private bool _shownBroken;
    private AudioStreamPlayer3D? _crackle;
    private bool _shown;

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        // The sparks and the glow go on top of the prop, whatever its height.
        float top = PropTop();
        GetNode<Node3D>("Sparks").Position = new Vector3(0f, top, 0f);
        GetNode<Node3D>("Glow").Position = new Vector3(0f, top + 0.3f, 0f);
    }

    // Sparks and a flickering glow, so a broken one is seen from down the street; it
    // leans while broken and springs back, with a puff, when fixed.
    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        GetNode<CpuParticles3D>("Sparks").Emitting = Broken;
        OmniLight3D glow = GetNode<OmniLight3D>("Glow");
        glow.Visible = Broken;

        if (Broken)
        {
            _flicker += delta;
            glow.LightEnergy = 1.5f + (1.5f * Mathf.Abs(Mathf.Sin((float)_flicker * 17f) * Mathf.Sin((float)_flicker * 5f)));
        }

        if (!_shown || Broken != _shownBroken)
        {
            // The sparks crackle while broken.
            if (Broken && _crackle == null)
            {
                _crackle = Audio.AudioDirector.Current?.Attach("fx.sparks", this);
            }
            else if (!Broken && _crackle != null)
            {
                _crackle.QueueFree();
                _crackle = null;
            }

            ShowState(_shown);
            _shown = true;
            _shownBroken = Broken;
        }
    }

    private void ShowState(bool animate)
    {
        // Only the model leans: the collision stays as the server has it.
        Node3D? model = GetNodeOrNull<Node3D>("Prop/Model");

        if (model == null)
        {
            return;
        }

        Vector3 lean = new Vector3(0f, 0f, Broken ? BrokenLean : 0f);

        if (!animate)
        {
            model.RotationDegrees = lean;
            return;
        }

        Tween tween = model.CreateTween();
        tween.TweenProperty(model, "rotation_degrees", lean, Broken ? 0.2 : 0.6).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

        if (!Broken)
        {
            Puff();
        }
    }

    // A one-off burst of pale green bits: fixed.
    private void Puff()
    {
        CpuParticles3D puff = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Amount = 24,
            Lifetime = 0.8,
            Explosiveness = 0.9f,
            Direction = Vector3.Up,
            Spread = 70f,
            InitialVelocityMin = 1.5f,
            InitialVelocityMax = 3f,
            Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f, RadialSegments = 6, Rings = 3, Material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.6f, 1f, 0.7f) } },
            Position = new Vector3(0f, PropTop(), 0f),
        };
        AddChild(puff);
        puff.Finished += puff.QueueFree;
    }

    private float PropTop()
    {
        CollisionShape3D? shape = GetNodeOrNull<CollisionShape3D>("Prop/Collision");
        BoxShape3D? box = shape?.Shape as BoxShape3D;

        if (shape == null || box == null)
        {
            return 1f;
        }

        return shape.Position.Y + (box.Size.Y / 2f);
    }
}
