namespace MmoGame3d.Gardening;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Gardening;
using MmoGame3d.Ui;

/// <summary>
/// The potting table, first person: a table, a pot on it, the pots to choose from along
/// the top and the pieces in a tray beside, each as a picture. Press a piece and it is in
/// the pot; drag it where it should go. While held, the wheel turns it, Shift+wheel leans
/// it out and Ctrl+wheel sizes it. Click a placed piece to move it again; drop it off the
/// soil to put it back. Right-drag walks round the table and the wheel moves in and out
/// when nothing is held; a key in the corner says so. The plant is only a design until
/// Complete sends it to the server.
/// </summary>
public partial class GardenScreen : Control
{
    public const string PieceGroup = "garden_piece";

    private const float TableScale = 1.6f;
    private const float TableTop = 0.8f * TableScale;
    private const float TurnStep = 0.2f;
    private const float TiltStep = 0.08f;
    private const float ScaleStep = 0.05f;

    // A placed piece is picked up by clicking within this much of its spot, as a fraction
    // of the soil's radius.
    private const float PickReach = 0.3f;

    private readonly PlantDesign _design = new PlantDesign();
    private readonly List<Node3D> _models = new List<Node3D>();

    private SubViewportContainer _picture = null!;
    private Camera3D _camera = null!;
    private Node3D _plant = null!;
    private Node3D? _pot;
    private Label _status = null!;
    private Label _count = null!;
    private GridContainer _pieceList = null!;
    private Control _banner = null!;
    private Control _help = null!;
    private Control _naming = null!;
    private Control _done = null!;
    private Button _complete = null!;
    private LineEdit _name = null!;
    private Label _doneText = null!;

    // The piece being moved, and its model; null when the hand is empty.
    private PlantPiece? _held;
    private Node3D? _heldModel;
    private bool _heldOnSoil;

    private float _orbit;
    private float _distance = 2.6f;
    private bool _orbiting;

    public event Action<string, string>? CompletePressed;
    public event Action? Closed;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        AddToGroup(Players.ChaseCamera.ScreenGroup);

        // Holding the focus keeps the walking keys off the body while at the table.
        FocusMode = FocusModeEnum.All;
        _design.Pot = PlantParts.Pots[1];
        BuildStage();
        BuildPanel();
        BuildPotBanner();
        BuildHelp();
        ChangePot(PlantParts.Pots[1]);
        CallDeferred(Control.MethodName.GrabFocus);
    }

    // The server made the plant: what it is, where it stands, and the reward.
    public void ShowMade(long plantId, string name, string reward)
    {
        _naming.Visible = false;
        _done.Visible = true;
        string title = name.Length > 0 ? "\"" + name + "\"" : "Your house plant";
        _doneText.Text =
            title + " is house plant #" + plantId + ", created by you, and kept on record for good.\n\n"
            + "It will be sold and placed somewhere in the world. For now it stands outside the greenhouse, where anyone can inspect it.\n\n"
            + "For your work: a " + reward + ", in your bag.";
    }

    public bool IsDone
    {
        get { return _done.Visible; }
    }

    // What the table shows: the pieces planted (its count line), and its status line.
    public int PieceCount
    {
        get { return _design.Pieces.Count; }
    }

    public string Status
    {
        get { return _status.Text; }
    }

    public void ShowProblem(string text)
    {
        _status.Text = text;
        _naming.Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (_done.Visible || _naming.Visible)
        {
            return;
        }

        InputEventMouseMotion? motion = @event as InputEventMouseMotion;
        InputEventMouseButton? button = @event as InputEventMouseButton;

        if (motion != null)
        {
            if (_held != null)
            {
                FollowMouse(motion.Position);
            }
            else if (_orbiting)
            {
                _orbit -= motion.Relative.X * 0.008f;
                PlaceCamera();
            }

            return;
        }

        if (button == null)
        {
            return;
        }

        if (_held != null)
        {
            HandleHeld(button);
            return;
        }

        switch (button.ButtonIndex)
        {
            case MouseButton.Right:
                _orbiting = button.Pressed && OverPicture(button.Position);
                break;
            case MouseButton.WheelUp:
                if (button.Pressed && OverPicture(button.Position))
                {
                    _distance = Mathf.Clamp(_distance - 0.15f, 1.2f, 5f);
                    PlaceCamera();
                }

                break;
            case MouseButton.WheelDown:
                if (button.Pressed && OverPicture(button.Position))
                {
                    _distance = Mathf.Clamp(_distance + 0.15f, 1.2f, 5f);
                    PlaceCamera();
                }

                break;
            case MouseButton.Left:
                if (button.Pressed && OverPicture(button.Position))
                {
                    PickUpPlaced(button.Position);
                }

                break;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();

            if (_naming.Visible)
            {
                _naming.Visible = false;
            }
            else
            {
                Closed?.Invoke();
            }
        }
    }

    // While a piece is held: the wheel shapes it, releasing the left button drops it.
    private void HandleHeld(InputEventMouseButton button)
    {
        GetViewport().SetInputAsHandled();

        switch (button.ButtonIndex)
        {
            case MouseButton.WheelUp:
            case MouseButton.WheelDown:
                if (!button.Pressed)
                {
                    return;
                }

                float sign = button.ButtonIndex == MouseButton.WheelUp ? 1f : -1f;

                if (button.CtrlPressed)
                {
                    _held!.Scale = Mathf.Clamp(_held.Scale + (sign * ScaleStep), PlantPiece.MinScale, PlantPiece.MaxScale);
                }
                else if (button.ShiftPressed)
                {
                    _held!.Tilt = Mathf.Clamp(_held.Tilt + (sign * TiltStep), 0f, PlantPiece.MaxTiltRadians);
                }
                else
                {
                    _held!.Yaw = Mathf.Wrap(_held.Yaw + (sign * TurnStep), -Mathf.Pi, Mathf.Pi);
                }

                FollowMouse(button.Position);
                break;
            case MouseButton.Left:
                if (!button.Pressed)
                {
                    Drop();
                }

                break;
        }
    }

    // A piece from the list goes into the hand.
    private void Take(string id)
    {
        if (_held != null || _design.Pieces.Count >= PlantDesign.MaxPieces)
        {
            return;
        }

        // Into the middle of the pot at once: released over the tray, it is planted there.
        _held = new PlantPiece { Id = id, Yaw = (float)GD.RandRange(-Mathf.Pi, Mathf.Pi) };
        _heldModel = PlantBuilder.Model(id);
        _heldOnSoil = true;

        if (_heldModel != null)
        {
            _plant.AddChild(_heldModel);
            PlantBuilder.Place(_heldModel, _design.Pot, _held);
        }

        _status.Text = "Release to plant it in the middle, or drag it where it should go.";
    }

    private void PickUpPlaced(Vector2 mouse)
    {
        Vector2? spot = SoilSpot(mouse);

        if (!spot.HasValue)
        {
            return;
        }

        int nearest = -1;
        float best = PickReach;

        for (int i = 0; i < _design.Pieces.Count; i++)
        {
            float distance = new Vector2(_design.Pieces[i].X, _design.Pieces[i].Z).DistanceTo(spot.Value);

            if (distance < best)
            {
                best = distance;
                nearest = i;
            }
        }

        if (nearest < 0)
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        _held = _design.Pieces[nearest];
        _heldModel = _models[nearest];
        _design.Pieces.RemoveAt(nearest);
        _models.RemoveAt(nearest);
        Refresh();
    }

    private void FollowMouse(Vector2 mouse)
    {
        // Over the tray or the pots it stays where it was.
        if (_held == null || !OverPicture(mouse))
        {
            return;
        }

        Vector2? spot = SoilSpot(mouse);

        if (!spot.HasValue)
        {
            return;
        }

        _heldOnSoil = spot.Value.Length() <= 1f;
        _held.X = spot.Value.X;
        _held.Z = spot.Value.Y;

        if (_heldModel != null)
        {
            PlantBuilder.Place(_heldModel, _design.Pot, _held);
        }

        _status.Text = _heldOnSoil ? "Release to plant it. Wheel turns, Shift+wheel leans, Ctrl+wheel sizes." : "Off the soil: release to put it back.";
    }

    private void Drop()
    {
        if (_held == null)
        {
            return;
        }

        if (_heldOnSoil)
        {
            _design.Pieces.Add(_held);

            if (_heldModel != null)
            {
                _models.Add(_heldModel);
            }
        }
        else
        {
            _heldModel?.QueueFree();
        }

        _held = null;
        _heldModel = null;
        _status.Text = "";
        Refresh();
    }

    // Where the mouse points on the soil's plane, in soil radii from the pot's middle.
    private Vector2? SoilSpot(Vector2 mouse)
    {
        Vector2 local = mouse - _picture.GlobalPosition;
        Vector3 from = _camera.ProjectRayOrigin(local);
        Vector3 along = _camera.ProjectRayNormal(local);
        float height = TableTop + PlantParts.SoilHeight(_design.Pot);

        if (Mathf.Abs(along.Y) < 0.0001f)
        {
            return null;
        }

        float t = (height - from.Y) / along.Y;

        if (t < 0f)
        {
            return null;
        }

        Vector3 hit = from + (along * t);
        float radius = PlantParts.SoilRadius(_design.Pot);
        return new Vector2(hit.X / radius, hit.Z / radius);
    }

    // Over the table itself, not the tray, the pots along the top or the key.
    private bool OverPicture(Vector2 mouse)
    {
        return mouse.X < GetNode<Control>("Panel").GlobalPosition.X
            && !_banner.GetGlobalRect().HasPoint(mouse)
            && !_help.GetGlobalRect().HasPoint(mouse);
    }

    private void ChangePot(string pot)
    {
        _design.Pot = pot;
        _pot?.QueueFree();
        _pot = PlantBuilder.Model(pot);

        if (_pot != null)
        {
            _plant.AddChild(_pot);
        }

        for (int i = 0; i < _design.Pieces.Count && i < _models.Count; i++)
        {
            PlantBuilder.Place(_models[i], pot, _design.Pieces[i]);
        }
    }

    private void Clear()
    {
        foreach (Node3D model in _models)
        {
            model.QueueFree();
        }

        _models.Clear();
        _design.Pieces.Clear();
        Refresh();
    }

    private void Refresh()
    {
        _count.Text = _design.Pieces.Count + " / " + PlantDesign.MaxPieces + " pieces";
        bool full = _design.Pieces.Count >= PlantDesign.MaxPieces;

        foreach (Node node in GetTree().GetNodesInGroup(PieceGroup))
        {
            ((Button)node).Disabled = full;
        }

        _complete.Disabled = _design.Pieces.Count == 0;
    }

    private void PlaceCamera()
    {
        Vector3 target = new Vector3(0f, TableTop + 0.6f, 0f);
        Vector3 eye = target + new Vector3(Mathf.Sin(_orbit) * _distance, _distance * 0.75f, Mathf.Cos(_orbit) * _distance);
        _camera.Transform = new Transform3D(Basis.LookingAt(target - eye, Vector3.Up), eye);
    }

    private void BuildStage()
    {
        _picture = new SubViewportContainer { Name = "Picture", Stretch = true };
        _picture.SetAnchorsPreset(LayoutPreset.FullRect);
        _picture.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_picture);

        Godot.Environment environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.72f, 0.84f, 0.76f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.9f, 0.95f, 0.9f),
            AmbientLightEnergy = 0.6f,
        };
        SubViewport viewport = new SubViewport { OwnWorld3D = true, World3D = new World3D { Environment = environment }, HandleInputLocally = false };
        _picture.AddChild(viewport);

        _camera = new Camera3D { Fov = 45f };
        viewport.AddChild(_camera);
        PlaceCamera();

        viewport.AddChild(new DirectionalLight3D { LightEnergy = 1.2f, ShadowEnabled = true, RotationDegrees = new Vector3(-55f, -35f, 0f) });

        PackedScene? table = ResourceLoader.Exists("res://game/props/furniture_bits/TableMedium.tscn") ? GD.Load<PackedScene>("res://game/props/furniture_bits/TableMedium.tscn") : null;

        if (table != null)
        {
            Node3D tableNode = table.Instantiate<Node3D>();
            tableNode.Scale = Vector3.One * TableScale;
            viewport.AddChild(tableNode);
        }

        _plant = new Node3D { Name = "Plant", Position = new Vector3(0f, TableTop, 0f) };
        viewport.AddChild(_plant);
    }

    private void BuildPanel()
    {
        PanelContainer panel = new PanelContainer { Name = "Panel", CustomMinimumSize = new Vector2(330, 0) };
        panel.SetAnchorsPreset(LayoutPreset.RightWide);
        panel.OffsetLeft = -330;
        AddChild(panel);
        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 14);
        }

        panel.AddChild(margin);
        VBoxContainer column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        margin.AddChild(column);

        Label title = new Label { Text = "Potting table" };
        title.AddThemeFontSizeOverride("font_size", 22);
        column.AddChild(title);
        column.AddChild(new Label { Text = "Choose a pot along the top, then up to five pieces from the tray.", AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1f, 1f, 1f, 0.7f) });

        column.AddChild(new Label { Text = "Pieces" });
        HFlowContainer tabs = new HFlowContainer();
        column.AddChild(tabs);
        ButtonGroup tabGroup = new ButtonGroup();

        for (int i = 0; i < PlantParts.PieceFamilies.Length; i++)
        {
            int family = i;
            Button tab = new Button { Text = PlantParts.PieceFamilies[i][0], ToggleMode = true, ButtonGroup = tabGroup, ButtonPressed = i == 0, FocusMode = FocusModeEnum.None };
            tab.Pressed += () => ShowFamily(family);
            tabs.AddChild(tab);
        }

        ScrollContainer scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        _pieceList = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _pieceList.AddThemeConstantOverride("h_separation", 6);
        _pieceList.AddThemeConstantOverride("v_separation", 6);
        scroll.AddChild(_pieceList);
        ShowFamily(0);

        _count = new Label();
        column.AddChild(_count);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1f, 0.9f, 0.6f) };
        column.AddChild(_status);

        HBoxContainer buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        Button clear = new Button { Text = "Clear", FocusMode = FocusModeEnum.None };
        clear.Pressed += Clear;
        Button complete = new Button { Text = "Complete", FocusMode = FocusModeEnum.None };
        _complete = complete;
        complete.Pressed += () =>
        {
            _naming.Visible = true;
            _name.GrabFocus();
        };
        Button leave = new Button { Text = "Leave", FocusMode = FocusModeEnum.None };
        leave.Pressed += () => Closed?.Invoke();
        buttons.AddChild(clear);
        buttons.AddChild(complete);
        buttons.AddChild(leave);
        column.AddChild(buttons);

        _naming = Dialog("Name your plant (you can leave it blank)", out VBoxContainer namingRows);
        LineEdit name = new LineEdit { MaxLength = PlantDesign.MaxNameLength, PlaceholderText = "A name, or nothing" };
        _name = name;
        namingRows.AddChild(name);
        HBoxContainer namingButtons = new HBoxContainer();
        Button finish = new Button { Text = "Finish the plant" };
        finish.Pressed += () => CompletePressed?.Invoke(_design.Format(), name.Text.Trim());
        name.TextSubmitted += text => CompletePressed?.Invoke(_design.Format(), text.Trim());
        Button back = new Button { Text = "Back" };
        back.Pressed += () => _naming.Visible = false;
        namingButtons.AddChild(finish);
        namingButtons.AddChild(back);
        namingRows.AddChild(namingButtons);

        _done = Dialog("Done", out VBoxContainer doneRows);
        Label doneText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(420, 0) };
        _doneText = doneText;
        doneRows.AddChild(doneText);
        Button close = new Button { Text = "Leave the table" };
        close.Pressed += () => Closed?.Invoke();
        doneRows.AddChild(close);
        Refresh();
    }

    private void ShowFamily(int index)
    {
        foreach (Node old in _pieceList.GetChildren())
        {
            _pieceList.RemoveChild(old);
            old.QueueFree();
        }

        string[] family = PlantParts.PieceFamilies[index];

        for (int i = 1; i < family.Length; i++)
        {
            string id = family[i];
            Button piece = PictureButton(id, new Vector2(92, 92));
            piece.AddToGroup(PieceGroup);
            piece.ButtonDown += () => Take(id);

            // "monstera_leaf_large_A" is shown as "large A": the tab says what it is.
            string[] words = id.Split('_');
            string shortName = words.Length >= 2 ? words[words.Length - 2] + " " + words[words.Length - 1] : id;
            Label caption = new Label { Text = shortName, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            caption.AddThemeFontSizeOverride("font_size", 12);
            caption.AddThemeConstantOverride("outline_size", 4);
            caption.SetAnchorsPreset(LayoutPreset.BottomWide);
            caption.OffsetTop = -18;
            piece.AddChild(caption);
            _pieceList.AddChild(piece);
        }

        if (_count != null)
        {
            Refresh();
        }
    }

    // A button showing a picture of a model, named in its tooltip.
    private static Button PictureButton(string id, Vector2 size)
    {
        Button button = new Button { CustomMinimumSize = size, TooltipText = PlantParts.Describe(id), FocusMode = FocusModeEnum.None };
        Node3D? model = PlantBuilder.Model(id);

        if (model != null)
        {
            ModelThumb thumb = new ModelThumb { Model = model };
            thumb.SetAnchorsPreset(LayoutPreset.FullRect);
            thumb.OffsetLeft = 4;
            thumb.OffsetTop = 4;
            thumb.OffsetRight = -4;
            thumb.OffsetBottom = -4;
            button.AddChild(thumb);
        }

        return button;
    }

    // The pots along the top, a picture each, the chosen one pressed.
    private void BuildPotBanner()
    {
        PanelContainer banner = new PanelContainer { Name = "Pots" };
        banner.Position = new Vector2(16, 12);
        AddChild(banner);
        _banner = banner;
        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 6);
        }

        banner.AddChild(margin);
        HBoxContainer row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        margin.AddChild(row);
        ButtonGroup group = new ButtonGroup();

        for (int i = 0; i < PlantParts.Pots.Length; i++)
        {
            string pot = PlantParts.Pots[i];

            // A gap between the styles; each comes in three sizes.
            if (i > 0 && i % 3 == 0)
            {
                row.AddChild(new Control { CustomMinimumSize = new Vector2(10, 0) });
            }

            Button button = PictureButton(pot, new Vector2(54, 62));
            Label size = new Label { Text = pot.Substring(pot.LastIndexOf('_') + 1), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            size.AddThemeFontSizeOverride("font_size", 11);
            size.AddThemeConstantOverride("outline_size", 4);
            size.SetAnchorsPreset(LayoutPreset.BottomWide);
            size.OffsetTop = -16;
            button.AddChild(size);
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.ButtonPressed = pot == _design.Pot;
            button.Pressed += () => ChangePot(pot);
            row.AddChild(button);
        }
    }

    // What the mouse does, in the bottom left: a drawn mouse and a line per part.
    private void BuildHelp()
    {
        PanelContainer help = new PanelContainer { Name = "Help", MouseFilter = MouseFilterEnum.Ignore };
        help.SetAnchorsPreset(LayoutPreset.BottomLeft);
        help.GrowVertical = GrowDirection.Begin;
        help.OffsetLeft = 16;
        help.OffsetBottom = -16;
        AddChild(help);
        _help = help;
        MarginContainer margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 10);
        }

        help.AddChild(margin);
        HBoxContainer row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        margin.AddChild(row);
        row.AddChild(new MouseSketch());
        VBoxContainer lines = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        row.AddChild(lines);
        lines.AddChild(HelpLine("Left: press a piece to put it in the pot; drag it to move it.", MouseSketch.LeftColor));
        lines.AddChild(HelpLine("Wheel, holding a piece: turn it. Shift leans it, Ctrl sizes it.", MouseSketch.WheelColor));
        lines.AddChild(HelpLine("Wheel, hand empty: closer or farther.", MouseSketch.WheelColor));
        lines.AddChild(HelpLine("Right: drag to walk round the table.", MouseSketch.RightColor));
    }

    private static Label HelpLine(string text, Color color)
    {
        Label line = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore };
        line.AddThemeColorOverride("font_color", color);
        line.AddThemeFontSizeOverride("font_size", 14);
        return line;
    }

    // A centred panel over the table, hidden until needed.
    private Control Dialog(string heading, out VBoxContainer rows)
    {
        CenterContainer center = new CenterContainer { Visible = false };
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        center.MouseFilter = MouseFilterEnum.Stop;
        AddChild(center);
        PanelContainer panel = new PanelContainer();
        center.AddChild(panel);
        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 18);
        }

        panel.AddChild(margin);
        rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 10);
        margin.AddChild(rows);
        Label title = new Label { Text = heading };
        title.AddThemeFontSizeOverride("font_size", 20);
        rows.AddChild(title);
        return center;
    }
}
