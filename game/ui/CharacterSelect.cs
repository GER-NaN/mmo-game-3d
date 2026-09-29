namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Players;

/// <summary>
/// The account's characters, one card per slot (world.md: two per account): play one,
/// or make one in an empty slot. Each card shows the character as they look. It only
/// reports what was pressed; ClientGame acts on it.
/// </summary>
public partial class CharacterSelect : Control
{

    public event Action<string>? PlayPressed;
    public event Action? CreatePressed;
    public event Action? BackPressed;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(new ColorRect { Color = new Color(0.08f, 0.09f, 0.11f), AnchorRight = 1, AnchorBottom = 1 });
    }

    public void ShowCharacters(string[] ids, string[] names, string[] looks, int[] levels, string[] titles, string message)
    {
        Node? old = GetNodeOrNull("Center");

        if (old != null)
        {
            RemoveChild(old);
            old.QueueFree();
        }

        CenterContainer center = new CenterContainer { Name = "Center" };
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);
        VBoxContainer rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 16);
        center.AddChild(rows);

        Label title = new Label { Text = "Choose a character", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 26);
        rows.AddChild(title);

        HBoxContainer cards = new HBoxContainer();
        cards.AddThemeConstantOverride("separation", 24);
        rows.AddChild(cards);

        for (int slot = 0; slot < Characters.SlotsPerAccount; slot++)
        {
            cards.AddChild(slot < ids.Length ? Card(ids[slot], names[slot], looks[slot], levels[slot], titles[slot]) : EmptyCard());
        }

        if (message.Length > 0)
        {
            Label problem = new Label { Text = message, HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.6f, 0.45f) };
            rows.AddChild(problem);
        }

        Button back = new Button { Text = "Back", SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        back.Pressed += () => BackPressed?.Invoke();
        rows.AddChild(back);
    }

    private Control Card(string id, string name, string look, int level, string title)
    {
        VBoxContainer card = CardBox();
        card.AddChild(Preview(look));
        Label nameLabel = new Label { Text = name, HorizontalAlignment = HorizontalAlignment.Center };
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        card.AddChild(nameLabel);
        card.AddChild(new Label { Text = "Level " + level, HorizontalAlignment = HorizontalAlignment.Center });
        card.AddChild(new Label { Text = title.Length > 0 ? title : "No career", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 1f, 1f, 0.7f) });
        Button play = new Button { Text = "Play" };
        play.Pressed += () => PlayPressed?.Invoke(id);
        card.AddChild(play);
        return Framed(card);
    }

    private Control EmptyCard()
    {
        VBoxContainer card = CardBox();
        card.AddChild(new Control { CustomMinimumSize = new Vector2(220, 260) });
        card.AddChild(new Label { Text = "Empty slot", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 1f, 1f, 0.6f) });
        Button create = new Button { Text = "Create a character" };
        create.Pressed += () => CreatePressed?.Invoke();
        card.AddChild(create);
        return Framed(card);
    }

    private static VBoxContainer CardBox()
    {
        VBoxContainer card = new VBoxContainer { CustomMinimumSize = new Vector2(240, 0) };
        card.AddThemeConstantOverride("separation", 6);
        return card;
    }

    private static PanelContainer Framed(Control content)
    {
        PanelContainer frame = new PanelContainer();
        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 12);
        }

        margin.AddChild(content);
        frame.AddChild(margin);
        return frame;
    }

    // A small still of the character in its own world, facing the viewer.
    private static Control Preview(string look)
    {
        SubViewportContainer picture = new SubViewportContainer { CustomMinimumSize = new Vector2(220, 260), Stretch = true };
        SubViewport viewport = new SubViewport
        {
            OwnWorld3D = true,
            Size = new Vector2I(220, 260),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        picture.AddChild(viewport);

        Godot.Environment environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.ClearColor,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.8f, 0.82f, 0.9f),
            AmbientLightEnergy = 0.7f,
        };
        viewport.AddChild(new WorldEnvironment { Environment = environment });

        Vector3 eye = new Vector3(0f, 1.1f, 3.2f);
        Camera3D camera = new Camera3D { Fov = 40f, Transform = new Transform3D(Basis.LookingAt(new Vector3(0f, 0.75f, 0f) - eye, Vector3.Up), eye) };
        viewport.AddChild(camera);

        DirectionalLight3D sun = new DirectionalLight3D { LightEnergy = 1.3f, RotationDegrees = new Vector3(-40f, -30f, 0f) };
        viewport.AddChild(sun);

        // The model faces -Z like a player; turned round, it faces the camera.
        CharacterModel model = new CharacterModel { Appearance = look, RotationDegrees = new Vector3(0f, 180f, 0f) };
        viewport.AddChild(model);
        return picture;
    }
}
