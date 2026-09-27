namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Players;
using MmoGame3d.Rules.Movement;
using MmoGame3d.Ui;
using MmoGame3d.Zones;

/// <summary>
/// What a bot can see and do. It looks at the screen and the scene as a person looks at
/// them, and it acts only through input (keys, clicks, typing: the events a keyboard and
/// a mouse make), so everything past the input is the real game. What is open is always
/// looked up, never remembered, so a bot that lost track of itself still sees the truth.
/// </summary>
public sealed class BotBody
{
    // Held this long, like a key press, so the game sees the key down and then up.
    private const double TapSeconds = 0.1;

    // Roughly facing: walk while turning. Further round: turn on the spot first.
    private const float FacingSlack = 0.12f;
    private const float WalkWhileTurning = 0.8f;

    private readonly Node _node;
    private double _interactHeld = -1;

    public BotBody(Node node, Random random)
    {
        _node = node;
        Random = random;
    }

    public Random Random { get; }

    // Paths round buildings in the current zone.
    public BotNavigation Navigation { get; } = new BotNavigation();

    // Its persona's pace: every pause and read is this many times as long.
    public double Pace { get; set; } = 1;

    private SceneTree Tree
    {
        get { return _node.GetTree(); }
    }

    // ---------------------------------------------------------------- senses

    public Player? Me
    {
        get { return Tree.GetFirstNodeInGroup(Player.LocalGroup) as Player; }
    }

    // A player's body stands in its zone's Players.
    public Zone? Zone
    {
        get
        {
            Player? me = Me;
            return me == null ? null : me.GetParent()?.GetParent() as Zone;
        }
    }

    public string ZoneId
    {
        get
        {
            Zone? zone = Zone;
            return zone == null ? "" : zone.ZoneId;
        }
    }

    // Whether a door's trigger lies up to this far straight ahead: what a person sees
    // and steps round when they do not mean to go in.
    public bool DoorAhead(float reach)
    {
        return DoorToward(0f, reach);
    }

    // The door whose trigger the body stands in, by name, or "".
    public string DoorIn()
    {
        Player? me = Me;
        Node? doors = Zone?.GetNodeOrNull("Doors");

        if (me == null || doors == null)
        {
            return "";
        }

        foreach (Node node in doors.GetChildren())
        {
            CollisionShape3D? shape = node.GetNodeOrNull<CollisionShape3D>("Shape");
            BoxShape3D? box = shape?.Shape as BoxShape3D;

            if (shape == null || box == null)
            {
                continue;
            }

            Vector3 local = shape.ToLocal(me.GlobalPosition);

            if (Mathf.Abs(local.X) < box.Size.X / 2f && Mathf.Abs(local.Z) < (box.Size.Z / 2f) + 0.5f)
            {
                return node.Name;
            }
        }

        return "";
    }

    // The same, off to one side: a quarter turn right is -Pi/2, left +Pi/2.
    public bool DoorToward(float turn, float reach)
    {
        Player? me = Me;
        Node? doors = Zone?.GetNodeOrNull("Doors");

        if (me == null || doors == null)
        {
            return false;
        }

        // Heading 0 faces -Z, and a positive heading is turned left.
        float heading = me.Heading + turn;
        Vector3 forward = new Vector3(-Mathf.Sin(heading), 0f, -Mathf.Cos(heading));

        foreach (Node node in doors.GetChildren())
        {
            CollisionShape3D? shape = node.GetNodeOrNull<CollisionShape3D>("Shape");
            BoxShape3D? box = shape?.Shape as BoxShape3D;

            if (shape == null || box == null)
            {
                continue;
            }

            for (float ahead = 0.5f; ahead <= reach; ahead += 0.5f)
            {
                Vector3 local = shape.ToLocal(me.GlobalPosition + (forward * ahead));

                // Widened by the body's half width, as the body touches it first.
                if (Mathf.Abs(local.X) < (box.Size.X / 2f) + 0.5f && Mathf.Abs(local.Z) < (box.Size.Z / 2f) + 0.5f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // The "[F] ..." line: what F would use now, or "" when nothing is in reach.
    public string Prompt
    {
        get
        {
            Label? label = Tree.GetFirstNodeInGroup(Hud.PromptGroup) as Label;
            return label != null && label.Visible ? label.Text : "";
        }
    }

    public bool IsOnline
    {
        get { return Tree.GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) != null; }
    }

    // A screen over the whole view (a terminal, the potting table) takes the keys: the
    // body cannot walk until it is closed.
    public bool CannotWalk
    {
        get { return IsOnline || IsOpen<GardenScreen>(); }
    }

    // The terminal's screen while online, the phone's or a fixed terminal's.
    public Control? OnlineScreen()
    {
        return Tree.GetFirstNodeInGroup(Ui.TerminalScreen.GoOfflineGroup)?.Owner as Control;
    }

    // The controls of this kind under a root that a person could click now: shown, not
    // disabled, and wholly inside the window.
    public List<T> OnScreen<T>(Node root)
        where T : Control
    {
        List<T> found = new List<T>();
        Rect2 window = _node.GetViewport().GetVisibleRect();

        foreach (Node node in root.FindChildren("*", "", true, false))
        {
            T? control = node as T;
            BaseButton? button = node as BaseButton;
            LineEdit? field = node as LineEdit;

            if (control != null && control.IsVisibleInTree() && window.Encloses(control.GetGlobalRect())
                && (button == null || !button.Disabled) && (field == null || field.Editable))
            {
                found.Add(control);
            }
        }

        return found;
    }

    public Button? Usable(string group)
    {
        foreach (Node node in Tree.GetNodesInGroup(group))
        {
            Button? button = node as Button;

            if (button != null && !button.Disabled && button.IsVisibleInTree())
            {
                return button;
            }
        }

        return null;
    }

    public List<Button> UsableAll(string group)
    {
        List<Button> buttons = new List<Button>();

        foreach (Node node in Tree.GetNodesInGroup(group))
        {
            Button? button = node as Button;

            if (button != null && !button.Disabled && button.IsVisibleInTree())
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    // ---------------------------------------------------------------- what it has

    private Client.ClientGame? Game
    {
        get { return _node.GetParent() as Client.ClientGame; }
    }

    public int Money
    {
        get { return Game?.Dollars ?? 0; }
    }

    // In the bag: a stack of it, or one loose.
    public bool Has(Rules.Items.ItemType type)
    {
        Client.ClientGame? game = Game;

        if (game == null)
        {
            return false;
        }

        foreach (Rules.Items.ItemStack stack in game.Stacks)
        {
            if (stack.Type == type && stack.Quantity > 0)
            {
                return true;
            }
        }

        foreach (Rules.Items.ItemInstance item in game.Instances)
        {
            if (item.Type == type && item.ParentId == null && item.Slot == null)
            {
                return true;
            }
        }

        return false;
    }

    public bool Wears(Rules.Items.ItemType type)
    {
        Client.ClientGame? game = Game;

        if (game == null)
        {
            return false;
        }

        foreach (Rules.Items.ItemInstance item in game.Instances)
        {
            if (item.Type == type && item.ParentId == null && item.Slot != null)
            {
                return true;
            }
        }

        return false;
    }

    // Anything in the bag the recycler would take: not worn.
    public bool HasSomethingToSell
    {
        get
        {
            Client.ClientGame? game = Game;
            return game != null && (game.Stacks.Count > 0 || Has(Rules.Items.ItemType.Battery) || Has(Rules.Items.ItemType.Phone) || Has(Rules.Items.ItemType.EmpEmitter));
        }
    }

    // The fullest battery in the bag, 0 to 100: a new one is full; -1 with none.
    public int SpareBatteryPercent
    {
        get
        {
            Client.ClientGame? game = Game;
            int best = -1;

            if (game == null)
            {
                return best;
            }

            foreach (Rules.Items.ItemStack stack in game.Stacks)
            {
                if (stack.Type == Rules.Items.ItemType.Battery && stack.Quantity > 0)
                {
                    best = 100;
                }
            }

            foreach (Rules.Items.ItemInstance item in game.Instances)
            {
                if (item.Type == Rules.Items.ItemType.Battery && item.IsLoose)
                {
                    best = Math.Max(best, Rules.Items.Power.Percent(item.Charge));
                }
            }

            return best;
        }
    }

    // The worn phone's charge, 0 to 100; -1 with no phone worn.
    public int PhonePercent
    {
        get
        {
            Client.ClientGame? game = Game;

            if (game == null)
            {
                return -1;
            }

            Rules.Items.Belongings mine = new Rules.Items.Belongings(new Rules.Items.Inventory(), new List<Rules.Items.ItemInstance>(game.Instances));

            if (mine.Equipped(Rules.Items.SlotType.Device) == null)
            {
                return -1;
            }

            Rules.Items.ItemInstance? battery = mine.DeviceBattery();
            return battery == null ? 0 : Rules.Items.Power.Percent(battery.Charge);
        }
    }

    // The items lying in this zone, less those it failed to reach before: a person
    // gives up on the one in the wall.
    public List<Node3D> GroundItems()
    {
        List<Node3D> items = Children<Items.GroundItem>("Items");
        items.RemoveAll(item => Unreachable.Contains(item.Name));
        return items;
    }

    // Items (by node name) a walk failed to reach.
    public HashSet<string> Unreachable { get; } = new HashSet<string>();

    public List<Node3D> LiveDrones()
    {
        List<Node3D> live = new List<Node3D>();

        foreach (Node3D node in Children<Drones.Drone>("Drones"))
        {
            if (!((Drones.Drone)node).Down)
            {
                live.Add(node);
            }
        }

        return live;
    }

    // The middle of every map cell of this zone not discovered yet, in world space.
    public List<Vector3> Undiscovered()
    {
        List<Vector3> spots = new List<Vector3>();
        Zone? zone = Zone;
        byte[]? cells = zone == null ? null : Game?.MapCells(zone.ZoneId);

        if (zone == null || cells == null || zone.MapSize == Vector2.Zero)
        {
            return spots;
        }

        Rules.Maps.Discovery map = new Rules.Maps.Discovery(zone.MapSize.X, zone.MapSize.Y);
        map.Load(cells);

        for (int row = 0; row < map.Rows; row++)
        {
            for (int column = 0; column < map.Columns; column++)
            {
                if (!map.IsDiscovered(column, row))
                {
                    float x = (-map.Width / 2f) + ((column + 0.5f) * Rules.Maps.Discovery.CellSize);
                    float z = (-map.Depth / 2f) + ((row + 0.5f) * Rules.Maps.Discovery.CellSize);
                    spots.Add(zone.ToGlobal(new Vector3(x, 0f, z)));
                }
            }
        }

        return spots;
    }

    public T? Nearest<T>(List<T> things)
        where T : Node3D
    {
        T? nearest = null;

        foreach (T thing in things)
        {
            if (nearest == null || DistanceTo(thing.GlobalPosition) < DistanceTo(nearest.GlobalPosition))
            {
                nearest = thing;
            }
        }

        return nearest;
    }

    private List<Node3D> Children<T>(string parent)
        where T : Node3D
    {
        List<Node3D> found = new List<Node3D>();
        Node? under = Zone?.GetNodeOrNull(parent);

        if (under == null || !under.IsInsideTree())
        {
            return found;
        }

        foreach (Node node in under.GetChildren())
        {
            T? thing = node as T;

            if (thing != null && thing.IsInsideTree())
            {
                found.Add(thing);
            }
        }

        return found;
    }

    // The group's button on the row that names the item ("Buy" beside "EMP Emitter").
    public Button? RowButton(string group, string itemName)
    {
        foreach (Button button in UsableAll(group))
        {
            Node? row = button.GetParent();

            if (row == null)
            {
                continue;
            }

            foreach (Node cell in row.GetChildren())
            {
                Label? label = cell as Label;

                if (label != null && label.Text.Contains(itemName))
                {
                    return button;
                }
            }
        }

        return null;
    }

    // Something standing in the current zone, by its path under the zone.
    public Node3D? Thing(string path)
    {
        // A zone on its way out is out of the tree before it is freed.
        Zone? zone = Zone;
        Node3D? thing = zone == null || !zone.IsInsideTree() ? null : zone.GetNodeOrNull<Node3D>(path);
        return thing != null && thing.IsInsideTree() ? thing : null;
    }

    public float DistanceTo(Vector3 point)
    {
        Player? me = Me;

        if (me == null)
        {
            return float.MaxValue;
        }

        Vector3 apart = point - me.GlobalPosition;
        apart.Y = 0f;
        return apart.Length();
    }

    // Everything open over the world: panels, screens, the game menu.
    public List<Control> OpenPanels()
    {
        List<Control> open = new List<Control>();
        Node? ui = _node.GetParent()?.GetNodeOrNull("Ui");

        if (ui == null)
        {
            return open;
        }

        foreach (Node node in ui.FindChildren("*", "Control", true, false))
        {
            Control? control = node as Control;

            if (control != null && IsPanel(control) && control.IsVisibleInTree())
            {
                open.Add(control);
            }
        }

        return open;
    }

    public bool IsOpen<T>()
        where T : Control
    {
        foreach (Control panel in OpenPanels())
        {
            if (panel is T)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPanel(Control control)
    {
        return control is InventoryPanel || control is SkillsPanel || control is SocialPanel || control is MapPanel
            || control is InGameMenu || control is SettingsPanel || control is ShopPanel || control is WorkbenchPanel
            || control is GivePanel || control is RecyclerPanel || control is CollegePanel || control is VisitorBookPanel
            || control is GardenScreen || control is PlantCard || control is CharacterCreator;
    }

    // ---------------------------------------------------------------- hands

    // Called every frame, so a tap of F lets go after its moment.
    // Seconds since this body appeared. The server spawns a new body at login and at each
    // zone change, so this is also the time since arriving: a finding a few seconds in
    // points at arriving, not at the place.
    public double BodyAge { get; private set; }

    private ulong _bodyId;

    public void Tick(double delta)
    {
        Player? me = Me;
        ulong id = me == null ? 0 : me.GetInstanceId();
        BodyAge = id == _bodyId ? BodyAge + delta : 0;
        _bodyId = id;

        if (_interactHeld >= 0)
        {
            _interactHeld += delta;

            if (_interactHeld >= TapSeconds)
            {
                _interactHeld = -1;
                Input.ActionRelease("interact");
            }
        }
    }

    public void Interact()
    {
        Stop();
        Input.ActionPress("interact");
        _interactHeld = 0;
    }

    public void Click(Button button)
    {
        BotDriver.Click(button.GetGlobalRect().GetCenter());
    }

    // A key as an input event, for what the game takes from events (menus, the bag,
    // the phone) rather than from polled actions.
    public static void Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
    }

    // Opens the chat and types a line: talk, or an emote ("/wave").
    public void Chat(string text)
    {
        Stop();
        Press("chat");
        BotDriver.Type(text);
    }

    public void Stop()
    {
        Input.ActionRelease("move_forward");
        Input.ActionRelease("move_back");
        Input.ActionRelease("turn_left");
        Input.ActionRelease("turn_right");
        Input.ActionRelease("jump");
    }

    // Turns toward a point and walks once roughly facing it. The distance left.
    public float SteerTo(Vector3 target)
    {
        Player? me = Me;

        if (me == null)
        {
            return float.MaxValue;
        }

        // A focused text field takes the keys; walking needs them.
        _node.GetViewport().GuiReleaseFocus();
        Vector3 apart = target - me.GlobalPosition;
        apart.Y = 0f;

        // Heading 0 faces -Z, and a positive heading is turned left (Walking).
        float wanted = Mathf.Atan2(-apart.X, -apart.Z);
        float off = Walking.WrapAngle(wanted - me.Heading);
        Input.ActionRelease("turn_left");
        Input.ActionRelease("turn_right");

        if (off > FacingSlack)
        {
            Input.ActionPress("turn_left");
        }
        else if (off < -FacingSlack)
        {
            Input.ActionPress("turn_right");
        }

        if (Mathf.Abs(off) < WalkWhileTurning)
        {
            Input.ActionPress("move_forward");
        }
        else
        {
            Input.ActionRelease("move_forward");
        }

        return apart.Length();
    }

    // One step toward nothing open: goes offline, closes the game menu or a settings
    // screen with their buttons, a panel with its own key, anything else with Esc. True
    // when nothing is open. Esc on the world opens the game menu, so it is never pressed
    // unless something Esc closes is open.
    public bool CloseOne()
    {
        if (IsOnline)
        {
            Button? goOffline = Tree.GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) as Button;

            if (goOffline != null && _node.GetViewport().GetVisibleRect().Encloses(goOffline.GetGlobalRect()))
            {
                Click(goOffline);
            }
            else
            {
                Press("ui_cancel");
            }

            return false;
        }

        List<Control> open = OpenPanels();

        // A text field (the chat line) takes the keys, a panel's own key too: let go of
        // it first, as a person clicks away.
        Control? focus = _node.GetViewport().GuiGetFocusOwner();

        if (open.Count > 0 && (focus is LineEdit || focus is TextEdit))
        {
            _node.GetViewport().GuiReleaseFocus();
            return false;
        }

        // The top one first: later in the tree draws over earlier, as the map does over
        // the game menu's Resume.
        open.Reverse();

        foreach (Control panel in open)
        {
            if (panel is InventoryPanel)
            {
                Press("inventory");
            }
            else if (panel is SkillsPanel)
            {
                Press("skills");
            }
            else if (panel is SocialPanel)
            {
                Press("social");
            }
            else if (panel is MapPanel)
            {
                Press("map");
            }
            else if (panel is SettingsPanel)
            {
                Button? back = Usable(SettingsPanel.BackGroup);

                if (back != null)
                {
                    Click(back);
                }
            }
            else if (panel is CharacterCreator)
            {
                Button? cancel = Usable(CharacterCreator.CancelGroup);

                if (cancel != null)
                {
                    Click(cancel);
                }
            }
            else if (panel is InGameMenu)
            {
                Button? resume = Usable(InGameMenu.ResumeGroup);

                if (resume != null)
                {
                    Click(resume);
                }
            }
            else
            {
                Press("ui_cancel");
            }

            return false;
        }

        return true;
    }
}
