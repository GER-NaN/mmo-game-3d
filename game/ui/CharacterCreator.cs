namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Players;

/// <summary>
/// Making a character, or changing how one looks (the wardrobe): a turning preview in
/// its own small world, and the choices beside it. The name is asked only for a new
/// character; it is chosen once. The preview is the same CharacterModel the world uses,
/// so what you see is what others see.
/// </summary>
public partial class CharacterCreator : Control
{
    // Colours, the backpack and the glasses fit the Protagonists only, so they are hidden
    // until customizing is designed for every base (TODO.md); the code stays.
    private static readonly bool Customizing = false;

    private const float TurnRadiansPerSecond = 0.6f;

    private readonly Random _random = new Random();
    private Appearance _appearance = new Appearance();
    private CharacterModel? _preview;
    private bool _naming;

    // (name, appearance): the name is "" in the wardrobe.
    public event Action<string, string>? DonePressed;
    public event Action? CancelPressed;

    public void Open(bool naming, string name, string appearance)
    {
        _naming = naming;
        _appearance = Appearance.Parse(appearance);

        // A little above the face, looking at the chest; the light from the front left.
        Camera3D camera = GetNode<Camera3D>("%Camera");
        camera.Position = new Vector3(0f, 1.3f, 3.4f);
        camera.LookAt(new Vector3(0f, 0.8f, 0f), Vector3.Up);
        GetNode<DirectionalLight3D>("%Sun").RotationDegrees = new Vector3(-40f, -30f, 0f);
        GetNode<Label>("%Title").Text = naming ? "A new character" : "Wardrobe";
        GetNode<Control>("%NameRow").Visible = naming;
        GetNode<LineEdit>("%Name").Text = name;
        GetNode<Button>("%Done").Text = naming ? "Create" : "Save";

        OptionButton bases = GetNode<OptionButton>("%Base");

        foreach (string id in Looks.Ids)
        {
            bases.AddItem(Looks.NameOf(id));
            bases.SetItemMetadata(bases.ItemCount - 1, id);

            if (id == _appearance.Base)
            {
                bases.Select(bases.ItemCount - 1);
            }
        }

        bases.ItemSelected += index => ChooseBase((int)index);

        // Through the bases one at a time, to see each on the turning preview.
        GetNode<Button>("%BaseBack").Pressed += () => ChooseBase((bases.Selected + bases.ItemCount - 1) % bases.ItemCount);
        GetNode<Button>("%BaseNext").Pressed += () => ChooseBase((bases.Selected + 1) % bases.ItemCount);

        Stepper("%Skin", Appearance.SkinTones, () => _appearance.Skin, value => _appearance.Skin = value);
        Stepper("%Hair", Appearance.HairColors, () => _appearance.Hair, value => _appearance.Hair = value);
        Stepper("%Top", Appearance.ClothesColors, () => _appearance.Top, value => _appearance.Top = value);
        Stepper("%Bottom", Appearance.ClothesColors, () => _appearance.Bottom, value => _appearance.Bottom = value);

        CheckBox backpack = GetNode<CheckBox>("%Backpack");
        backpack.ButtonPressed = _appearance.Backpack;
        backpack.Toggled += on =>
        {
            _appearance.Backpack = on;
            Changed();
        };

        CheckBox glasses = GetNode<CheckBox>("%Glasses");
        glasses.ButtonPressed = _appearance.Glasses;
        glasses.Toggled += on =>
        {
            _appearance.Glasses = on;
            Changed();
        };

        GetNode<Control>("%Grid").Visible = Customizing;
        backpack.Visible = Customizing;
        glasses.Visible = Customizing;

        GetNode<Button>("%Random").Pressed += Randomize;
        GetNode<Button>("%Done").Pressed += () => DonePressed?.Invoke(_naming ? GetNode<LineEdit>("%Name").Text.Trim() : "", _appearance.Format());
        GetNode<Button>("%Cancel").Pressed += () => CancelPressed?.Invoke();
        Changed();
    }

    public void SetStatus(string text)
    {
        GetNode<Label>("%Status").Text = text;
    }

    public override void _Process(double delta)
    {
        _preview?.RotateY(TurnRadiansPerSecond * (float)delta);
    }

    // A row of "<  colour  >": the swatch shows the colour, or "Original".
    private void Stepper(string row, string[] colors, Func<int> get, Action<int> set)
    {
        HBoxContainer box = GetNode<HBoxContainer>(row);
        Button back = new Button { Text = "<", FocusMode = FocusModeEnum.None };
        Button next = new Button { Text = ">", FocusMode = FocusModeEnum.None };
        ColorRect swatch = new ColorRect { CustomMinimumSize = new Vector2(90, 24) };
        Label original = new Label { Text = "Original", HorizontalAlignment = HorizontalAlignment.Center };
        original.SetAnchorsPreset(LayoutPreset.FullRect);
        swatch.AddChild(original);
        box.AddChild(back);
        box.AddChild(swatch);
        box.AddChild(next);

        Action show = () =>
        {
            string color = colors[get()];
            swatch.Color = color.Length > 0 ? new Color(color) : new Color(0.25f, 0.25f, 0.28f);
            original.Visible = color.Length == 0;
        };

        back.Pressed += () =>
        {
            set((get() + colors.Length - 1) % colors.Length);
            show();
            Changed();
        };
        next.Pressed += () =>
        {
            set((get() + 1) % colors.Length);
            show();
            Changed();
        };
        show();
        _refreshers.Add(show);
    }

    private readonly List<Action> _refreshers = new List<Action>();

    private void ChooseBase(int index)
    {
        OptionButton bases = GetNode<OptionButton>("%Base");
        bases.Select(index);
        _appearance.Base = (string)bases.GetItemMetadata(index);
        Changed();
    }

    private void Randomize()
    {
        OptionButton bases = GetNode<OptionButton>("%Base");
        ChooseBase(_random.Next(bases.ItemCount));

        if (!Customizing)
        {
            return;
        }

        _appearance.Skin = _random.Next(Appearance.SkinTones.Length);
        _appearance.Hair = _random.Next(Appearance.HairColors.Length);
        _appearance.Top = _random.Next(Appearance.ClothesColors.Length);
        _appearance.Bottom = _random.Next(Appearance.ClothesColors.Length);
        _appearance.Backpack = _random.Next(2) == 0;
        GetNode<CheckBox>("%Backpack").SetPressedNoSignal(_appearance.Backpack);

        foreach (Action refresh in _refreshers)
        {
            refresh();
        }

        Changed();
    }

    // The preview is rebuilt: a model is dressed once, as it enters the tree.
    private void Changed()
    {
        Node3D stage = GetNode<Node3D>("%Stage");
        float turn = _preview != null ? _preview.Rotation.Y : Mathf.Pi;

        if (_preview != null)
        {
            stage.RemoveChild(_preview);
            _preview.QueueFree();
        }

        _preview = new CharacterModel { Name = "Preview", Appearance = _appearance.Format() };
        stage.AddChild(_preview);
        _preview.Rotation = new Vector3(0f, turn, 0f);
    }
}
