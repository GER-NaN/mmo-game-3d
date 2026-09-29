namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// The bot's senses and hands, shared by every step. Senses read the client: its
/// screens, its player, its zone, its view of the player's state. Hands act only as a
/// player can: keys pressed, held and typed through the input system, and clicks through
/// the viewport (bots.md F2, T1). Every random choice comes from Random, seeded from
/// bot.json, so a run can be played again (R4).
/// </summary>
public class BotBody
{
    private readonly Node _node;
    private readonly string _folder;
    private readonly HashSet<string> _held = new HashSet<string>();
    private Zone? _zone;

    public BotBody(Node node, string folder, BotEventLog events, int seed)
    {
        _node = node;
        _folder = folder;
        Events = events;
        Random = new Random(seed);
    }

    public BotEventLog Events { get; }

    public Random Random { get; }

    public BotNavigator Navigator { get; } = new BotNavigator();

    // The activity being played, for steps that add to it (a need met by another).
    public BotActivityRun? Run { get; set; }

    // The thing the last approach walked up to, for a check of what became of it (fixed,
    // by this bot or another).
    public Interact.Interactable? LastApproached { get; set; }

    // How many needs are being met inside each other; a need that needs itself would
    // otherwise never end.
    public int NeedDepth { get; set; }

    // The local player's body while it is in the tree. It is made anew in each zone, and
    // on a zone change the old one leaves the tree before it is freed.
    public Player? Player
    {
        get
        {
            Player? player = _node.GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Player;
            return player != null && player.IsInsideTree() ? player : null;
        }
    }

    // The zone this client has loaded; a client holds one at a time, under World.
    public Zone? Zone
    {
        get
        {
            if (_zone != null && GodotObject.IsInstanceValid(_zone) && _zone.IsInsideTree())
            {
                return _zone;
            }

            _zone = null;
            Node? world = _node.GetTree().Root.GetNodeOrNull("Main/World");

            if (world == null)
            {
                return null;
            }

            foreach (Node holder in world.GetChildren())
            {
                Zone? zone = holder.GetNodeOrNull<Zone>("Zone");

                if (zone != null)
                {
                    _zone = zone;
                    break;
                }
            }

            return _zone;
        }
    }

    // What the client knows of its player: zone, money, belongings, open screens. Null
    // before the game has started.
    public ClientView? View
    {
        get
        {
            ClientGame? client = _node.GetTree().Root.GetNodeOrNull<ClientGame>("Main/ClientGame");
            return client?.View;
        }
    }

    // The other players in this zone, as this client sees them.
    public List<Player> OthersHere()
    {
        List<Player> others = new List<Player>();
        Zone? zone = Zone;

        if (zone == null)
        {
            return others;
        }

        foreach (Node child in zone.Players.GetChildren())
        {
            Player? other = child as Player;

            if (other != null && other.IsInsideTree() && !other.IsInGroup(Players.Player.LocalGroup))
            {
                others.Add(other);
            }
        }

        return others;
    }

    // The first node in a group, as this type, or null: Old Town's TownState, say.
    public T? InGroup<T>(string group)
        where T : Node
    {
        return _node.GetTree().GetFirstNodeInGroup(group) as T;
    }

    // The first visible screen of this type, or null.
    public T? Find<T>()
        where T : Control
    {
        return FindVisible<T>(_node.GetTree().Root);
    }

    // A real press and release at the control's centre, through the viewport, so the
    // click goes the way a player's does: a hidden, disabled or covered control does not
    // press.
    public void Click(Control control)
    {
        ClickAt(control, control.Size / 2);
    }

    // A click at a point inside a control, in its own coordinates: a spot on a picture.
    public void ClickAt(Control control, Vector2 local)
    {
        ClickScreen(control.GetViewport(), control.GetGlobalTransformWithCanvas() * local);
    }

    // A click at a point in the window, on whatever is there: a player in the world, say.
    // The mouse moves there first, as a player's does, so the window knows what it is over.
    public void ClickScreen(Viewport viewport, Vector2 point)
    {
        InputEventMouseMotion move = new InputEventMouseMotion();
        move.Position = point;
        move.GlobalPosition = point;
        viewport.PushInput(move, true);

        InputEventMouseButton press = new InputEventMouseButton();
        press.ButtonIndex = MouseButton.Left;
        press.Position = point;
        press.GlobalPosition = point;
        press.Pressed = true;
        viewport.PushInput(press, true);

        InputEventMouseButton release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        viewport.PushInput(release, true);
    }

    // One turn of the mouse wheel over a control's middle, down or up.
    public void Wheel(Control over, bool down)
    {
        Vector2 point = over.GetGlobalTransformWithCanvas() * (over.Size / 2);
        Viewport viewport = over.GetViewport();

        InputEventMouseMotion move = new InputEventMouseMotion();
        move.Position = point;
        move.GlobalPosition = point;
        viewport.PushInput(move, true);

        InputEventMouseButton wheel = new InputEventMouseButton();
        wheel.ButtonIndex = down ? MouseButton.WheelDown : MouseButton.WheelUp;
        wheel.Position = point;
        wheel.GlobalPosition = point;
        wheel.Factor = 1f;
        wheel.Pressed = true;
        viewport.PushInput(wheel, true);

        InputEventMouseButton release = (InputEventMouseButton)wheel.Duplicate();
        release.Pressed = false;
        viewport.PushInput(release, true);
    }

    // The event a key bound to the action makes, down or up, fed through the input system.
    public void Key(string action, bool pressed)
    {
        InputEventAction key = new InputEventAction();
        key.Action = action;
        key.Pressed = pressed;
        Input.ParseInputEvent(key);
    }

    // A key on the keyboard, down or up, whatever action it is bound to.
    public void RawKey(Godot.Key key, bool pressed)
    {
        InputEventKey raw = new InputEventKey();
        raw.Keycode = key;
        raw.PhysicalKeycode = key;
        raw.Pressed = pressed;
        Input.ParseInputEvent(raw);
    }

    // A key held down or let go, for walking and turning.
    public void Hold(string action, bool down)
    {
        if (down && !_held.Contains(action))
        {
            Input.ActionPress(action);
            _held.Add(action);
        }
        else if (!down && _held.Contains(action))
        {
            Input.ActionRelease(action);
            _held.Remove(action);
        }
    }

    public void ReleaseAll()
    {
        foreach (string action in new List<string>(_held))
        {
            Hold(action, false);
        }
    }

    // Text typed into whatever has the keys, over what it held, a key per character, then
    // Enter.
    public void Type(string text)
    {
        // Ctrl+A first, so the line replaces what the field held (a name filled in for you).
        InputEventKey selectAll = new InputEventKey();
        selectAll.Keycode = Godot.Key.A;
        selectAll.PhysicalKeycode = Godot.Key.A;
        selectAll.CtrlPressed = true;
        selectAll.Pressed = true;
        Input.ParseInputEvent(selectAll);

        InputEventKey selectAllUp = (InputEventKey)selectAll.Duplicate();
        selectAllUp.Pressed = false;
        Input.ParseInputEvent(selectAllUp);

        foreach (char letter in text)
        {
            InputEventKey key = new InputEventKey();
            key.Unicode = letter;
            key.Pressed = true;
            Input.ParseInputEvent(key);
        }

        InputEventKey enter = new InputEventKey();
        enter.Keycode = Godot.Key.Enter;
        enter.PhysicalKeycode = Godot.Key.Enter;
        enter.Pressed = true;
        Input.ParseInputEvent(enter);

        InputEventKey up = (InputEventKey)enter.Duplicate();
        up.Pressed = false;
        Input.ParseInputEvent(up);
    }

    // The notices that came after the given count (ClientView.NoticeCount), oldest first.
    // Only the newest are kept, so a very old count gets what is left.
    public List<string> NoticesSince(int count)
    {
        List<string> fresh = new List<string>();
        ClientView? view = View;

        if (view == null)
        {
            return fresh;
        }

        int first = view.NoticeCount - view.Notices.Count;

        for (int i = 0; i < view.Notices.Count; i++)
        {
            if (first + i >= count)
            {
                fresh.Add(view.Notices[i]);
            }
        }

        return fresh;
    }

    // The text field that has the keys, or null.
    public LineEdit? FocusedField()
    {
        return _node.GetViewport().GuiGetFocusOwner() as LineEdit;
    }

    // Nothing else has the keys: no text field, no full screen.
    public bool KeysFree()
    {
        return _node.GetViewport().GuiGetFocusOwner() == null && _node.GetTree().GetNodeCountInGroup(ChaseCamera.ScreenGroup) == 0;
    }

    // What has the keys, for a finding: the focused control or a full screen, else
    // "free"; and the walking keys down, which cancel out when opposite ones are.
    public string KeysHeldBy()
    {
        Control? focus = _node.GetViewport().GuiGetFocusOwner();
        Godot.Collections.Array<Node> screens = _node.GetTree().GetNodesInGroup(ChaseCamera.ScreenGroup);
        string holder = "free";

        if (focus != null)
        {
            holder = focus.GetPath();
        }
        else if (screens.Count > 0)
        {
            holder = screens[0].GetPath();
        }

        List<string> down = new List<string>();

        foreach (string action in new[] { "move_forward", "move_back", "turn_left", "turn_right", "strafe_left", "strafe_right" })
        {
            if (Input.IsActionPressed(action))
            {
                down.Add(action);
            }
        }

        return holder + ", down: " + (down.Count > 0 ? string.Join(" ", down) : "none");
    }

    // Where the bot is, for a finding or a failure: "town (12.0, 0.0, -3.5)".
    public string Where()
    {
        Player? player = Player;
        Zone? zone = Zone;

        if (player == null || zone == null)
        {
            return "not in the world";
        }

        Vector3 at = zone.ToLocal(player.GlobalPosition);
        return zone.ZoneId + " (" + at.X.ToString("0.0") + ", " + at.Y.ToString("0.0") + ", " + at.Z.ToString("0.0") + ")";
    }

    // What the window shows now, into the execution's folder. A headless client draws
    // nothing, so it has no picture.
    public void SavePicture(string fileName)
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        _node.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(_folder, fileName));
        Events.Write("screenshot", fileName);
    }

    private static T? FindVisible<T>(Node node)
        where T : Control
    {
        T? found = node as T;

        if (found != null && found.IsVisibleInTree())
        {
            return found;
        }

        foreach (Node child in node.GetChildren())
        {
            T? inChild = FindVisible<T>(child);

            if (inChild != null)
            {
                return inChild;
            }
        }

        return null;
    }
}
