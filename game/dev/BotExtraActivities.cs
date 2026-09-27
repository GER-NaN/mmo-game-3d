namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// The activities only some personas do (BotPersonas): poking at every screen, running
/// for the edge of the world, squeezing into gaps.
/// </summary>
public static class BotExtraActivities
{
    public static readonly BotActivity PokeAround = new BotActivity("poke around", 8, body => body.Zone != null && !body.ZoneId.StartsWith("taxi"), body => new List<BotStep>
    {
        new PokeStep(false, 25 + (body.Random.NextDouble() * 20)),
        new CloseAllStep(),
    });

    public static readonly BotActivity PokeAtTerminal = new BotActivity("poke at a terminal", 5, body => body.ZoneId == ZoneIds.Town, body =>
    {
        string terminal = body.Random.Next(2) == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
        return new List<BotStep>
        {
            new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
            new UseStep("Go Online", b => b.IsOnline),
            new PokeStep(true, 25 + (body.Random.NextDouble() * 20)),
            new CloseAllStep(),
        };
    });

    public static readonly BotActivity RunForTheEdge = new BotActivity("run for the edge", 8, body => body.Zone != null && !body.ZoneId.StartsWith("taxi"), body => new List<BotStep>
    {
        new EdgeStep(45),
    });

    public static readonly BotActivity MashKeys = new BotActivity("mash the keys", 10, body => body.Zone != null, body => new List<BotStep>
    {
        new MashStep(10 + (body.Random.NextDouble() * 15)),
        new CloseAllStep(),
    });

    public static readonly BotActivity SqueezeIntoAGap = new BotActivity("squeeze into a gap", 8, body => body.Zone?.GetNodeOrNull("Buildings") != null, body => new List<BotStep>
    {
        new SqueezeStep(),
    });
}

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
    private static readonly string[] Lines = { "hello", "testing 123", "this is my plan", "lfg", "brb" };

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
