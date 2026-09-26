namespace MmoGame3d.Players;

using Godot;

/// <summary>
/// What a player looks like on a client: a KayKit character with the medium rig's
/// animations. The characters ship without animations; the rig files carry them, and
/// both share the same node tree (Rig_Medium/Skeleton3D), so an animation from a rig
/// file plays on any character as it is. The library is built once and shared.
///
/// The server never makes one: it has no screen, and bodies there are capsules only.
/// </summary>
public partial class CharacterModel : Node3D
{
    public const string Idle = "Idle_A";
    public const string Walk = "Walking_A";
    public const string Run = "Running_A";
    public const string Airborne = "Jump_Idle";
    public const string Busy = "Interact";

    // Placeholders until chosen: which character a player is, and how big.
    public const string PlayerModel = "res://assets/kaykit/characters/Protagonist_A.glb";
    private const float ModelScale = 0.8f;
    private const float BlendSeconds = 0.2f;

    private static readonly string[] RigFiles =
    {
        "res://assets/kaykit/character_animations/rig_medium/Rig_Medium_General.glb",
        "res://assets/kaykit/character_animations/rig_medium/Rig_Medium_MovementBasic.glb",
    };

    // Animations that repeat until something else plays. glTF has no loop flag, so the
    // import leaves every animation playing once.
    private static readonly string[] Looping = { Idle, Walk, Run, Airborne, Busy };

    private static AnimationLibrary? _library;

    private AnimationPlayer? _animations;
    private string _playing = "";

    // Set before the node enters the tree.
    public string ModelPath { get; set; } = PlayerModel;

    public override void _Ready()
    {
        PackedScene? scene = ResourceLoader.Exists(ModelPath) ? GD.Load<PackedScene>(ModelPath) : null;

        // Without the art (a machine it was not copied to), the capsule stays.
        if (scene == null)
        {
            GD.PrintErr("Character art missing at " + ModelPath + "; see assets/README.md");
            return;
        }

        Node3D model = scene.Instantiate<Node3D>();
        model.Scale = Vector3.One * ModelScale;

        // KayKit characters face +Z; a player's front is -Z.
        model.RotateY(Mathf.Pi);
        AddChild(model);

        _animations = new AnimationPlayer { Name = "Animations" };
        model.AddChild(_animations);
        _animations.AddAnimationLibrary("", SharedLibrary());
        Play(Idle);
    }

    public void Play(string animation)
    {
        if (_animations == null || animation == _playing || !_animations.HasAnimation(animation))
        {
            return;
        }

        _playing = animation;
        _animations.Play(animation, BlendSeconds);
    }

    private static AnimationLibrary SharedLibrary()
    {
        if (_library != null)
        {
            return _library;
        }

        _library = new AnimationLibrary();

        foreach (string file in RigFiles)
        {
            if (!ResourceLoader.Exists(file))
            {
                continue;
            }

            Node rig = GD.Load<PackedScene>(file).Instantiate();
            AnimationPlayer? source = rig.FindChild("AnimationPlayer", true, false) as AnimationPlayer;

            if (source != null)
            {
                foreach (StringName name in source.GetAnimationList())
                {
                    Animation animation = (Animation)source.GetAnimation(name).Duplicate();

                    if (System.Array.IndexOf(Looping, name.ToString()) >= 0)
                    {
                        animation.LoopMode = Animation.LoopModeEnum.Linear;
                    }

                    if (!_library.HasAnimation(name))
                    {
                        _library.AddAnimation(name, animation);
                    }
                }
            }

            rig.Free();
        }

        return _library;
    }
}
