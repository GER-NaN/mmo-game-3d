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

    private bool _shown;
    private bool _shownFull;

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        GetNode<Node3D>("Full").Visible = HasItem;
        GetNode<Node3D>("Empty").Visible = !HasItem;

        // Emptied while in view: a moment for it, not only a swap of models.
        if (_shown && _shownFull && !HasItem)
        {
            ShowOpened();
        }

        _shown = true;
        _shownFull = HasItem;
    }

    // The empty chest bounces open and a handful of bright bits fly out. A placeholder look.
    private void ShowOpened()
    {
        Audio.AudioDirector.Current?.PlayAt("fx.chest", GlobalPosition);
        Node3D empty = GetNode<Node3D>("Empty");
        empty.Scale = new Vector3(1.15f, 0.8f, 1.15f);
        Tween tween = empty.CreateTween();
        tween.TweenProperty(empty, "scale", Vector3.One, 0.5).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

        CpuParticles3D burst = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Amount = 20,
            Lifetime = 0.9,
            Explosiveness = 0.95f,
            Direction = Vector3.Up,
            Spread = 35f,
            InitialVelocityMin = 2.5f,
            InitialVelocityMax = 4f,
            Mesh = new BoxMesh { Size = new Vector3(0.07f, 0.07f, 0.07f), Material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.85f, 0.35f) } },
            Position = new Vector3(0f, 0.6f, 0f),
        };
        AddChild(burst);
        burst.Finished += burst.QueueFree;
    }
}
