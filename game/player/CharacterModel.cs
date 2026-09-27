namespace MmoGame3d.Players;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Players;

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
        "res://assets/kaykit/character_animations/rig_medium/Rig_Medium_Simulation.glb",
        "res://assets/kaykit/character_animations/rig_medium/Rig_Medium_Tools.glb",
    };

    // Animations that repeat until something else plays. glTF has no loop flag, so the
    // import leaves every animation playing once.
    private static readonly string[] Looping =
    {
        Idle, Walk, Run, Airborne, Busy, "Waving", "Cheering", "Sit_Floor_Idle", "Push_Ups", "Hammering", "Working_A",
    };

    private static readonly Shader RecolorShader = GD.Load<Shader>("res://game/player/Recolor.gdshader");

    // Which palette cells (row * 8 + column) skin, hair, top and bottom use in each base
    // look, found from the meshes' UVs. A cell is shared between parts (the cap and the
    // jacket both use white), so each choice is also limited to the meshes it belongs
    // to: hair to the head, the top to body and arms, the bottom to legs and waist.
    private static readonly Dictionary<string, int[][]> PartCells = new Dictionary<string, int[][]>
    {
        { "a", new[] { new[] { 0 }, new[] { 1 }, new[] { 9 }, new[] { 8 } } },
        { "b", new[] { new[] { 0 }, new[] { 1 }, new[] { 13 }, new[] { 12 } } },
    };

    private static readonly string[][] PartMeshes =
    {
        new[] { "" },
        new[] { "_Head" },
        new[] { "_Body", "_ArmLeft", "_ArmRight" },
        new[] { "_Body", "_LegLeft", "_LegRight" },
    };

    // A palette cell's average lightness, per texture, for keeping its shading.
    private static readonly Dictionary<string, float[]> CellLightness = new Dictionary<string, float[]>();

    private static AnimationLibrary? _library;

    // The phone the character holds while online on it; made on first use.
    private Node3D? _phone;
    private BoneAttachment3D? _toolHand;
    private string _tool = "";
    private OmniLight3D? _glow;
    private double _flicker;

    private AnimationPlayer? _animations;
    private string _playing = "";

    // Set before the node enters the tree.
    public string ModelPath { get; set; } = PlayerModel;

    // A player's appearance (see Appearance): when set, it picks the model and recolours
    // it, and ModelPath is ignored. Set before the node enters the tree.
    public string Appearance { get; set; } = "";

    // The model for a player's look. Placeholders until character creation is designed.
    public static string PathFor(string look)
    {
        switch (Rules.Players.Appearance.Parse(look).Base)
        {
            case "b":
                return "res://assets/kaykit/characters/Protagonist_B.glb";
            default:
                return PlayerModel;
        }
    }

    public override void _Ready()
    {
        Appearance? appearance = Appearance.Length > 0 ? Rules.Players.Appearance.Parse(Appearance) : null;

        if (appearance != null)
        {
            ModelPath = PathFor(appearance.Base);
        }

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

        if (appearance != null)
        {
            Dress(model, appearance);
        }

        _animations = new AnimationPlayer { Name = "Animations" };
        model.AddChild(_animations);
        _animations.AddAnimationLibrary("", SharedLibrary());
        Play(Idle);
    }

    // Accessories on or off, and the chosen colours on the palette cells of each part.
    private static void Dress(Node3D model, Appearance appearance)
    {
        int[][] cells;

        if (!PartCells.TryGetValue(appearance.Base, out cells!))
        {
            return;
        }

        string[] colors =
        {
            Rules.Players.Appearance.SkinTones[appearance.Skin],
            Rules.Players.Appearance.HairColors[appearance.Hair],
            Rules.Players.Appearance.ClothesColors[appearance.Top],
            Rules.Players.Appearance.ClothesColors[appearance.Bottom],
        };

        foreach (Node node in model.FindChildren("*", "MeshInstance3D", true, false))
        {
            MeshInstance3D mesh = (MeshInstance3D)node;
            string name = mesh.Name.ToString();

            if (name.EndsWith("_Backpack"))
            {
                mesh.Visible = appearance.Backpack;
            }
            else if (name.EndsWith("_Glasses"))
            {
                mesh.Visible = appearance.Glasses;
            }

            for (int surface = 0; surface < mesh.GetSurfaceOverrideMaterialCount(); surface++)
            {
                StandardMaterial3D? original = mesh.Mesh.SurfaceGetMaterial(surface) as StandardMaterial3D;

                if (original == null || original.AlbedoTexture == null)
                {
                    continue;
                }

                float[] lightness = Lightness(original.AlbedoTexture);
                List<int> slotCells = new List<int>();
                List<Color> slotColors = new List<Color>();
                List<float> references = new List<float>();

                for (int part = 0; part < colors.Length; part++)
                {
                    if (colors[part].Length == 0 || !Wears(name, PartMeshes[part]))
                    {
                        continue;
                    }

                    foreach (int cell in cells[part])
                    {
                        slotCells.Add(cell);
                        slotColors.Add(new Color(colors[part]));
                        references.Add(lightness[cell]);
                    }
                }

                if (slotCells.Count == 0)
                {
                    continue;
                }

                ShaderMaterial material = new ShaderMaterial { Shader = RecolorShader };
                material.SetShaderParameter("albedo_texture", original.AlbedoTexture);
                material.SetShaderParameter("cells", slotCells.ToArray());
                material.SetShaderParameter("colors", slotColors.ToArray());
                material.SetShaderParameter("references", references.ToArray());
                material.SetShaderParameter("count", slotCells.Count);
                mesh.SetSurfaceOverrideMaterial(surface, material);
            }
        }
    }

    private static bool Wears(string meshName, string[] suffixes)
    {
        foreach (string suffix in suffixes)
        {
            if (meshName.EndsWith(suffix))
            {
                return true;
            }
        }

        return false;
    }

    private static float[] Lightness(Texture2D texture)
    {
        string key = texture.ResourcePath;
        float[]? cached;

        if (CellLightness.TryGetValue(key, out cached))
        {
            return cached;
        }

        float[] lightness = new float[32];
        Image image = texture.GetImage();

        if (image.IsCompressed())
        {
            image.Decompress();
        }

        int width = image.GetWidth();
        int height = image.GetHeight();

        for (int cell = 0; cell < 32; cell++)
        {
            int column = cell % 8;
            int row = cell / 8;
            float sum = 0f;
            int samples = 0;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    Color pixel = image.GetPixel((int)((column + ((x + 0.5f) / 4f)) * width / 8f), (int)((row + ((y + 0.5f) / 8f)) * height / 4f));
                    sum += (0.299f * pixel.R) + (0.587f * pixel.G) + (0.114f * pixel.B);
                    samples++;
                }
            }

            lightness[cell] = sum / samples;
        }

        CellLightness[key] = lightness;
        return lightness;
    }

    // The phone in the right hand, its screen light shifting colour like a TV on a wall.
    // The flicker means nothing; it only shows the player is on their phone.
    public void ShowPhone(bool on)
    {
        if (_phone == null && on)
        {
            Skeleton3D? skeleton = FindChild("Skeleton3D", true, false) as Skeleton3D;

            if (skeleton == null)
            {
                return;
            }

            BoneAttachment3D hand = new BoneAttachment3D { BoneName = "handslot.r" };
            skeleton.AddChild(hand);
            _phone = new Node3D { Name = "Phone" };
            hand.AddChild(_phone);
            _phone.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.09f, 0.17f, 0.015f), Material = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.1f, 0.12f) } },
            });
            _phone.AddChild(new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(0.075f, 0.14f), Material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.6f, 0.85f, 1f) } },
                Position = new Vector3(0f, 0f, 0.009f),
            });
            _glow = new OmniLight3D { LightEnergy = 0.8f, OmniRange = 1.2f, Position = new Vector3(0f, 0f, 0.15f) };
            _phone.AddChild(_glow);
        }

        if (_phone == null)
        {
            return;
        }

        _phone.Visible = on;

        if (on && _glow != null)
        {
            _flicker += GetProcessDeltaTime();
            float hue = (float)((Mathf.Sin(_flicker * 0.9) * 0.5) + 0.5) * 0.35f + 0.5f;
            _glow.LightColor = Color.FromHsv(hue, 0.5f, 1f);
            _glow.LightEnergy = 0.6f + (0.4f * Mathf.Abs(Mathf.Sin((float)_flicker * 7f)));
        }
    }

    // A tool from the KayKit tools pack in the right hand, by name; empty puts it away.
    public void ShowTool(string tool)
    {
        if (tool == _tool)
        {
            return;
        }

        _tool = tool;

        if (_toolHand == null)
        {
            Skeleton3D? skeleton = FindChild("Skeleton3D", true, false) as Skeleton3D;

            if (skeleton == null)
            {
                return;
            }

            _toolHand = new BoneAttachment3D { Name = "ToolHand", BoneName = "handslot.r" };
            skeleton.AddChild(_toolHand);
        }

        foreach (Node child in _toolHand.GetChildren())
        {
            child.QueueFree();
        }

        PackedScene? model = tool.Length > 0 ? ResourceLoader.Load<PackedScene>(ToolPath + tool + ".gltf") : null;

        if (model != null)
        {
            Node3D held = model.Instantiate<Node3D>();
            held.Scale = Vector3.One * ToolScale;
            _toolHand.AddChild(held);
        }
    }

    private const string ToolPath = "res://assets/kaykit/rpg_tools_bits/assets/";

    // The pack's tools are prop-sized (the hammer is 0.8 m); in the hand they are made
    // hand-sized. A placeholder until seen.
    private const float ToolScale = 0.4f;

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
