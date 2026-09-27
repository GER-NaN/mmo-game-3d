namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// Pokes at whatever is open: clicks a visible button at random, types into a text field
/// now and then, closes a panel after a few clicks and opens another (the bag, skills,
/// friends, the map, or the thing in reach). Online, it pokes at the terminal's screen
/// instead. Never a button that quits, leaves for the main menu or deletes.
/// </summary>
public sealed class PokeStep : BotStep
{
    private static readonly string[] Opens = { "inventory", "skills", "social", "map" };
    private static readonly string[] Denied = { "Quit", "main menu", "Leave to", "Delete", "Go Offline" };
    // Ordinary lines, and lines a careless or hostile player types: markup, other
    // scripts and emoji, far too long, blank, format strings, quotes for a database.
    public static readonly string[] Lines =
    {
        "hello", "testing 123", "this is my plan", "lfg", "brb",
        "[color=red]red[/color] [b]bold[/b] [url]x[/url]",
        "Åsa Ñoño 日本語 Привет 🙂🔥",
        new string('W', 400),
        new string('W', 119) + "🙂 past the cut",
        "   ",
        "%s %d {0} {{1}} \\n $name",
        "'; DROP TABLE players; --",
    };

    private readonly bool _terminal;
    private readonly double _seconds;
    private double _left;
    private double _next;
    private int _clicksHere;

    public PokeStep(bool terminal, double seconds)
        : base(terminal ? "poke at the terminal" : "poke around", seconds + 10)
    {
        _terminal = terminal;
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _next = 0;
        _clicksHere = 0;
        body.Stop();
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            return StepResult.Done;
        }

        if (_terminal && !body.IsOnline)
        {
            return StepResult.Failed;
        }

        _next -= delta;

        if (_next > 0)
        {
            return StepResult.Running;
        }

        _next = (0.8 + body.Random.NextDouble()) * body.Pace;
        Node? root = _terminal ? body.OnlineScreen() : body.OpenPanels().Find(p => !(p is InGameMenu) && !(p is SettingsPanel));

        if (root == null)
        {
            Open(body);
            return StepResult.Running;
        }

        if (_clicksHere >= 3 + body.Random.Next(4) && !_terminal)
        {
            _clicksHere = 0;
            body.CloseOne();
            return StepResult.Running;
        }

        List<LineEdit> fields = body.OnScreen<LineEdit>(root);

        if (fields.Count > 0 && body.Random.Next(4) == 0)
        {
            LineEdit field = fields[body.Random.Next(fields.Count)];
            string line = Lines[body.Random.Next(Lines.Length)];
            GD.Print("Bot: typing \"" + line + "\" into " + field.Name);
            BotDriver.Click(field.GetGlobalRect().GetCenter());
            BotDriver.Type(line);
            _clicksHere++;
            return StepResult.Running;
        }

        List<BaseButton> buttons = body.OnScreen<BaseButton>(root);
        buttons.RemoveAll(IsDenied);

        if (buttons.Count == 0)
        {
            body.CloseOne();
            return StepResult.Running;
        }

        BaseButton pick = buttons[body.Random.Next(buttons.Count)];
        GD.Print("Bot: poking " + Label(pick) + " in " + root.Name);
        BotDriver.Click(pick.GetGlobalRect().GetCenter());
        _clicksHere++;
        return StepResult.Running;
    }

    private static void Open(BotBody body)
    {
        if (body.Prompt.Length > 0 && !body.Prompt.Contains("Go Online") && body.Random.Next(3) == 0)
        {
            GD.Print("Bot: poking at \"" + body.Prompt + "\"");
            body.Interact();
            return;
        }

        string key = Opens[body.Random.Next(Opens.Length)];
        GD.Print("Bot: opening " + key);
        BotBody.Press(key);
    }

    private static bool IsDenied(BaseButton button)
    {
        string label = Label(button);

        foreach (string denied in Denied)
        {
            if (label.Contains(denied, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Label(BaseButton button)
    {
        Button? plain = button as Button;
        return plain != null && plain.Text.Length > 0 ? "\"" + plain.Text + "\"" : button.Name.ToString();
    }
}

/// <summary>
/// Presses game keys at random, several a second: walking, jumping, using, the phone, the
/// bag, the map, Esc and Enter. Keys only, never a click, so it cannot press Quit; the
/// keeper closes a game menu it leaves open.
/// </summary>
public sealed class MashStep : BotStep
{
    private static readonly string[] Taps = { "interact", "phone", "inventory", "map", "skills", "social", "emp", "ui_cancel", "chat", "ui_accept" };
    private static readonly string[] Holds = { "move_forward", "move_back", "turn_left", "turn_right", "strafe_left", "strafe_right", "jump" };

    private readonly double _seconds;
    private double _left;
    private double _next;

    public MashStep(double seconds)
        : base("mash the keys", seconds + 5)
    {
        _seconds = seconds;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _next = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        _next -= delta;

        if (_next > 0)
        {
            return StepResult.Running;
        }

        _next = 0.1 + (body.Random.NextDouble() * 0.3);

        if (body.Random.Next(3) == 0)
        {
            string key = Taps[body.Random.Next(Taps.Length)];
            GD.Print("Bot: mashing " + key);
            BotBody.Press(key);
        }
        else
        {
            string key = Holds[body.Random.Next(Holds.Length)];

            if (Input.IsActionPressed(key))
            {
                Input.ActionRelease(key);
            }
            else
            {
                Input.ActionPress(key);
            }
        }

        return StepResult.Running;
    }
}

/// <summary>
/// Runs straight for a point well past the zone's edge, jumping, to find a way out of
/// the world. The position judge reports it if one works (out-of-bounds, no-footing).
/// </summary>
public sealed class EdgeStep : BotStep
{
    private readonly double _seconds;
    private double _left;
    private Vector3 _toward;
    private double _jumpIn;

    public EdgeStep(double seconds)
        : base("run for the edge", seconds + 5)
    {
        _seconds = seconds;
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override bool Presses
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _toward;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        Zones.Zone? zone = body.Zone;
        float reach = zone == null || zone.MapSize == Vector2.Zero ? 40f : (Mathf.Max(zone.MapSize.X, zone.MapSize.Y) / 2f) + 40f;
        float angle = (float)(body.Random.NextDouble() * Mathf.Tau);
        Vector3 middle = zone == null ? Vector3.Zero : zone.GlobalPosition;
        _toward = middle + (new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach);
        GD.Print("Bot: running for the edge toward (" + _toward.X.ToString("0") + ", " + _toward.Z.ToString("0") + ")");
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        // Straight at it, never round: the point is to press against what stops a player.
        body.SteerTo(_toward);
        _jumpIn -= delta;

        if (_jumpIn <= 0)
        {
            _jumpIn = 1 + body.Random.NextDouble() * 2;
            Input.ActionPress("jump");
        }
        else
        {
            Input.ActionRelease("jump");
        }

        return StepResult.Running;
    }
}

/// <summary>
/// Walks straight into the gap between a building and its nearest neighbour and keeps
/// pushing a while: where a player can get wedged, the judges report it.
/// </summary>
public sealed class SqueezeStep : BotStep
{
    private const double PushFor = 20;

    private Vector3? _gap;
    private double _left;

    public SqueezeStep()
        : base("squeeze into a gap", PushFor + 10)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override bool Presses
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _gap;
    }

    public override void Begin(BotBody body)
    {
        _left = PushFor;
        _gap = null;
        Node? buildings = body.Zone?.GetNodeOrNull("Buildings");

        if (buildings == null || buildings.GetChildCount() < 2)
        {
            return;
        }

        List<Node3D> all = new List<Node3D>();

        foreach (Node node in buildings.GetChildren())
        {
            Node3D? building = node as Node3D;

            if (building != null)
            {
                all.Add(building);
            }
        }

        Node3D first = all[body.Random.Next(all.Count)];
        Node3D? nearest = null;

        foreach (Node3D other in all)
        {
            if (other != first && (nearest == null || other.GlobalPosition.DistanceTo(first.GlobalPosition) < nearest.GlobalPosition.DistanceTo(first.GlobalPosition)))
            {
                nearest = other;
            }
        }

        if (nearest != null)
        {
            _gap = (first.GlobalPosition + nearest.GlobalPosition) / 2f;
            GD.Print("Bot: squeezing between " + first.Name + " and " + nearest.Name);
        }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_gap == null)
        {
            return StepResult.Failed;
        }

        _left -= delta;

        if (_left <= 0)
        {
            body.Stop();
            return StepResult.Done;
        }

        body.SteerTo(_gap.Value);
        return StepResult.Running;
    }
}

/// <summary>
/// Follows another player at arm's length and uses what they use, through doors too:
/// two players on one terminal, one shopkeeper, one door at the same moment, arriving on
/// the same spot.
/// </summary>
public sealed class ShadowStep : BotStep
{
    private const float Close = 1.5f;
    private const float DoorReach = 5f;

    private readonly double _seconds;
    private double _left;
    private string _name = "";
    private Player? _other;
    private Vector3 _lastSeen;
    private DoorStep? _door;
    private Walker _walker = new Walker();
    private double _useIn;
    private double _closeIn = -1;
    private bool _walking;

    public ShadowStep(double seconds)
        : base("shadow someone", seconds + 10)
    {
        _seconds = seconds;
    }

    // Only while closing in or following: at the elbow of someone standing still, the
    // shadow stands still too.
    public override bool Walks
    {
        get { return _walking; }
    }

    public override bool MovesZone
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _other != null && GodotObject.IsInstanceValid(_other) ? _other.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _left = _seconds;
        _walker = new Walker();
        _door = null;
        _useIn = 2;
        _closeIn = -1;
        _other = Pick(body, "");
        _name = _other?.DisplayName ?? "";

        if (_other != null)
        {
            GD.Print("Bot: shadowing " + _name);
        }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _left -= delta;
        _walking = false;

        if (_left <= 0 || body.Me == null)
        {
            body.Stop();
            return _name.Length > 0 ? StepResult.Done : StepResult.Failed;
        }

        // A panel the last use opened: a moment in it, then away, as the other goes on.
        if (_closeIn >= 0)
        {
            _closeIn -= delta;

            if (_closeIn < 0)
            {
                body.CloseOne();
            }

            return StepResult.Running;
        }

        // After them through the door they took, as any door is taken (DoorStep); then
        // find them on the other side.
        if (_door != null)
        {
            _walking = true;
            StepResult through = _door.Tick(body, delta);

            if (through == StepResult.Running)
            {
                return StepResult.Running;
            }

            _door = null;
            _walker = new Walker();
            _other = through == StepResult.Done ? Pick(body, _name) : null;
            return _other == null ? StepResult.Done : StepResult.Running;
        }

        if (_other == null || !GodotObject.IsInstanceValid(_other) || !_other.IsInsideTree())
        {
            Node3D? door = NearestDoor(body, _lastSeen);

            if (door == null)
            {
                return _name.Length > 0 ? StepResult.Done : StepResult.Failed;
            }

            GD.Print("Bot: following " + _name + " through " + door.Name);
            _door = new DoorStep(door.Name);
            _door.Begin(body);
            _other = null;
            return StepResult.Running;
        }

        _lastSeen = _other.GlobalPosition;
        StepResult walked = _walker.Walk(body, _lastSeen, Close, delta);

        if (walked == StepResult.Failed)
        {
            _walker = new Walker();
        }

        if (walked != StepResult.Done)
        {
            _walking = true;
            return StepResult.Running;
        }

        // At their elbow: use whatever they are next to, now and then.
        _useIn -= delta;

        if (_useIn <= 0 && body.Prompt.Length > 0)
        {
            _useIn = 3 + (body.Random.NextDouble() * 4);
            GD.Print("Bot: using what " + _name + " is at: " + body.Prompt);
            body.Interact();
            _closeIn = 2 + body.Random.NextDouble() * 3;
        }

        return StepResult.Running;
    }

    // Another player in this zone: the one named, or anyone.
    private static Player? Pick(BotBody body, string name)
    {
        Player? me = body.Me;

        if (me == null)
        {
            return null;
        }

        List<Player> others = new List<Player>();

        foreach (Node node in me.GetParent().GetChildren())
        {
            Player? other = node as Player;

            if (other != null && other != me && (name.Length == 0 || other.DisplayName == name))
            {
                others.Add(other);
            }
        }

        return others.Count == 0 ? null : others[body.Random.Next(others.Count)];
    }

    private static Node3D? NearestDoor(BotBody body, Vector3 at)
    {
        Node? doors = body.Zone?.GetNodeOrNull("Doors");
        Node3D? nearest = null;

        foreach (Node node in doors?.GetChildren() ?? new Godot.Collections.Array<Node>())
        {
            Node3D? door = node as Node3D;

            if (door != null && door.GlobalPosition.DistanceTo(at) < DoorReach && (nearest == null || door.GlobalPosition.DistanceTo(at) < nearest.GlobalPosition.DistanceTo(at)))
            {
                nearest = door;
            }
        }

        return nearest;
    }
}
