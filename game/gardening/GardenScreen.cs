namespace MmoGame3d.Gardening;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Gardening;
using MmoGame3d.Ui;

/// <summary>
/// The potting table, first person: a table and a pot on it. First a pot is picked from
/// a carousel along the top (a click on the one in the middle); then that row gives way
/// to the pieces' carousel, up and down for the kind of plant. Press a piece and it is in
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
    // of the soil's radius, when the mouse is on no piece's box.
    private const float PickReach = 0.3f;

    // Far past the table, for the mouse's ray.
    private const float RayLength = 50f;

    // Laid over the piece the mouse is on, so it is plain which one a click takes.
    private static readonly StandardMaterial3D HoverTint = new StandardMaterial3D
    {
        AlbedoColor = new Color(1f, 1f, 0.7f, 0.35f),
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };

    private readonly PlantDesign _design = new PlantDesign();
    private readonly List<Node3D> _models = new List<Node3D>();

    private SubViewportContainer _picture = null!;
    private Camera3D _camera = null!;
    private Node3D _plant = null!;
    private Node3D? _pot;
    private Label _status = null!;
    private Label _count = null!;
    private static readonly PackedScene CarouselScene = GD.Load<PackedScene>("res://game/ui/Carousel.tscn");

    private Carousel _pieceList = null!;
    private Carousel _pots = null!;
    private Label _kindName = null!;
    private int _kind;
    private Control _potBanner = null!;
    private Control _leafBanner = null!;
    private Control _panel = null!;
    private Label _hint = null!;

    // The pot is picked: the pieces' carousel shows instead of the pots'.
    private bool _potPicked;
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

    // From the soil spot under the mouse to the held piece's spot, kept while it is
    // dragged, so it moves with the mouse instead of jumping under it. Null until the
    // first move over the table.
    private Vector2? _grab;

    // The placed piece the mouse is on, tinted; -1 for none.
    private int _hovered = -1;

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
        BuildLeafBanner();
        BuildHelp();
        ShowStep();
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
            else
            {
                Hover(OverPicture(motion.Position) ? PieceAt(motion.Position) : -1);
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
        _grab = null;

        if (_heldModel != null)
        {
            _plant.AddChild(_heldModel);
            PlantBuilder.Place(_heldModel, _design.Pot, _held);
        }

        _status.Text = "Release to plant it in the middle, or drag it where it should go.";
    }

    private void PickUpPlaced(Vector2 mouse)
    {
        int nearest = PieceAt(mouse);

        if (nearest < 0)
        {
            return;
        }

        Hover(-1);
        GetViewport().SetInputAsHandled();
        _held = _design.Pieces[nearest];
        _heldModel = _models[nearest];
        _grab = null;
        _design.Pieces.RemoveAt(nearest);
        _models.RemoveAt(nearest);
        Refresh();
    }

    // The placed piece under the mouse: of the pieces whose box the mouse's ray crosses,
    // the one whose middle is nearest the ray, so a leaf is taken by its tip as well as its
    // base. With none, a piece whose spot on the soil is near. -1 for none.
    private int PieceAt(Vector2 mouse)
    {
        Vector2 local = mouse - _picture.GlobalPosition;
        Vector3 from = _camera.ProjectRayOrigin(local);
        Vector3 along = _camera.ProjectRayNormal(local);
        int nearest = -1;
        float best = float.MaxValue;

        for (int i = 0; i < _models.Count; i++)
        {
            Aabb? box = Box(_models[i]);

            if (box == null || !box.Value.IntersectsSegment(from, from + (along * RayLength)))
            {
                continue;
            }

            Vector3 middle = box.Value.GetCenter();
            float off = (middle - from).Cross(along).Length();

            if (off < best)
            {
                best = off;
                nearest = i;
            }
        }

        if (nearest >= 0)
        {
            return nearest;
        }

        Vector2? spot = SoilSpot(mouse);

        if (!spot.HasValue)
        {
            return -1;
        }

        float reach = PickReach;

        for (int i = 0; i < _design.Pieces.Count; i++)
        {
            float distance = new Vector2(_design.Pieces[i].X, _design.Pieces[i].Z).DistanceTo(spot.Value);

            if (distance < reach)
            {
                reach = distance;
                nearest = i;
            }
        }

        return nearest;
    }

    // The box round every mesh of a model, in the table's world.
    private static Aabb? Box(Node3D model)
    {
        Aabb? box = null;

        foreach (Node node in model.FindChildren("*", "MeshInstance3D", true, false))
        {
            MeshInstance3D mesh = (MeshInstance3D)node;
            Aabb part = mesh.GlobalTransform * mesh.GetAabb();
            box = box == null ? part : box.Value.Merge(part);
        }

        return box;
    }

    private void Hover(int piece)
    {
        if (piece == _hovered)
        {
            return;
        }

        if (_hovered >= 0 && _hovered < _models.Count)
        {
            Tint(_models[_hovered], null);
        }

        _hovered = piece;

        if (piece >= 0)
        {
            Tint(_models[piece], HoverTint);
        }

        MouseDefaultCursorShape = piece >= 0 ? CursorShape.PointingHand : CursorShape.Arrow;
    }

    private static void Tint(Node3D model, Material? tint)
    {
        foreach (Node node in model.FindChildren("*", "MeshInstance3D", true, false))
        {
            ((MeshInstance3D)node).MaterialOverlay = tint;
        }
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

        if (_grab == null)
        {
            _grab = new Vector2(_held.X, _held.Z) - spot.Value;
        }

        Vector2 at = spot.Value + _grab.Value;
        _heldOnSoil = at.Length() <= 1f;
        _held.X = at.X;
        _held.Z = at.Y;

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

    // Over the table itself, not the carousel along the top, the panel or the key.
    private bool OverPicture(Vector2 mouse)
    {
        foreach (Control over in new[] { _potBanner, _leafBanner, _panel, _help })
        {
            if (over.Visible && over.GetGlobalRect().HasPoint(mouse))
            {
                return false;
            }
        }

        return true;
    }

    private void PickPot()
    {
        _potPicked = true;
        ShowStep();
    }

    // Back to the pots, with the pot empty.
    private void StartOver()
    {
        Clear();
        _potPicked = false;
        ShowStep();
    }

    private void ShowStep()
    {
        _potBanner.Visible = !_potPicked;
        _leafBanner.Visible = _potPicked;
        _hint.Text = _potPicked
            ? "Press a piece to put it in the pot, and drag it where it should go. Up to five."
            : "Pick a pot: step through them, then click the one in the middle.";
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
        Hover(-1);

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

    // What the table says and its buttons, floating in the bottom right.
    private void BuildPanel()
    {
        PanelContainer panel = new PanelContainer { Name = "Panel", CustomMinimumSize = new Vector2(300, 0) };
        panel.SetAnchorsPreset(LayoutPreset.BottomRight);
        panel.GrowHorizontal = GrowDirection.Begin;
        panel.GrowVertical = GrowDirection.Begin;
        panel.OffsetRight = -16;
        panel.OffsetBottom = -16;
        AddChild(panel);
        _panel = panel;
        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 12);
        }

        panel.AddChild(margin);
        VBoxContainer column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 6);
        margin.AddChild(column);

        Label title = new Label { Text = "Potting table" };
        title.AddThemeFontSizeOverride("font_size", 18);
        column.AddChild(title);
        _hint = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(276, 0), Modulate = new Color(1f, 1f, 1f, 0.7f) };
        column.AddChild(_hint);
        _count = new Label();
        column.AddChild(_count);
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(276, 0), Modulate = new Color(1f, 0.9f, 0.6f) };
        column.AddChild(_status);

        HBoxContainer buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        Button startOver = new Button { Text = "Start over", FocusMode = FocusModeEnum.None };
        startOver.Pressed += StartOver;
        Button complete = new Button { Text = "Complete", FocusMode = FocusModeEnum.None };
        _complete = complete;
        complete.Pressed += () =>
        {
            _naming.Visible = true;
            _name.GrabFocus();
        };
        Button leave = new Button { Text = "Leave", FocusMode = FocusModeEnum.None };
        leave.Pressed += () => Closed?.Invoke();
        buttons.AddChild(startOver);
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

    private Button KindButton(string name, string icon, string tooltip, int step)
    {
        Button button = new Button
        {
            Name = name,
            Icon = GD.Load<Texture2D>(icon),
            IconAlignment = HorizontalAlignment.Center,
            TooltipText = tooltip,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(36, 28),
        };
        button.Pressed += () => ShowFamily(_kind + step);
        return button;
    }

    // The kinds go round: past the last is the first again.
    private void ShowFamily(int index)
    {
        int count = PlantParts.PieceFamilies.Length;
        _kind = ((index % count) + count) % count;
        string[] family = PlantParts.PieceFamilies[_kind];
        _kindName.Text = "Type: " + family[0];
        List<Control> pieces = new List<Control>();

        for (int i = 1; i < family.Length; i++)
        {
            string id = family[i];
            Button piece = PictureButton(id, new Vector2(112, 112));
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
            pieces.Add(piece);
        }

        _pieceList.SetItems(pieces, 0);

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

    // A row along the top, centred, with no panel behind, so it floats over the table.
    private Control Banner(string name, string heading, out VBoxContainer rows)
    {
        PanelContainer banner = new PanelContainer { Name = name };
        banner.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        banner.SetAnchorsPreset(LayoutPreset.CenterTop);
        banner.GrowHorizontal = GrowDirection.Both;
        banner.OffsetTop = 12;
        AddChild(banner);
        rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 6);
        banner.AddChild(rows);
        Label title = new Label { Text = heading, HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeConstantOverride("outline_size", 8);
        title.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.12f, 0.1f));
        rows.AddChild(title);
        return banner;
    }

    // Step one: the pots, a picture each; a click on the one in the middle picks it.
    private void BuildPotBanner()
    {
        VBoxContainer rows;
        _potBanner = Banner("Pots", "Pick a pot", out rows);
        _pots = CarouselScene.Instantiate<Carousel>();
        _pots.Name = "PotCarousel";
        _pots.ShowCount = 5;
        rows.AddChild(_pots);
        List<Control> pots = new List<Control>();

        foreach (string pot in PlantParts.Pots)
        {
            Button button = PictureButton(pot, new Vector2(104, 116));
            Label size = new Label { Text = pot.Substring(pot.LastIndexOf('_') + 1), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            size.AddThemeFontSizeOverride("font_size", 13);
            size.AddThemeConstantOverride("outline_size", 4);
            size.SetAnchorsPreset(LayoutPreset.BottomWide);
            size.OffsetTop = -20;
            button.AddChild(size);
            pots.Add(button);
        }

        _pots.SetItems(pots, Array.IndexOf(PlantParts.Pots, _design.Pot));
        _pots.ChosenChanged += index => ChangePot(PlantParts.Pots[index]);
        _pots.ChosenPressed += index => PickPot();
    }

    // Step two: the pieces, with the kind of plant stepped up and down in the corner.
    private void BuildLeafBanner()
    {
        VBoxContainer rows;
        _leafBanner = Banner("Leaves", "Add your leaves", out rows);
        HBoxContainer kindRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        kindRow.AddThemeConstantOverride("separation", 6);
        kindRow.AddChild(KindButton("KindUp", "res://game/ui/icons/chevron-up.svg", "The kind before", -1));
        kindRow.AddChild(KindButton("KindDown", "res://game/ui/icons/chevron-down.svg", "The next kind", 1));
        _kindName = new Label();
        _kindName.AddThemeFontSizeOverride("font_size", 18);
        _kindName.AddThemeConstantOverride("outline_size", 6);
        _kindName.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.12f, 0.1f));
        kindRow.AddChild(_kindName);
        rows.AddChild(kindRow);
        _pieceList = CarouselScene.Instantiate<Carousel>();
        _pieceList.Name = "PieceCarousel";
        _pieceList.ShowCount = 5;
        rows.AddChild(_pieceList);
        ShowFamily(0);
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
