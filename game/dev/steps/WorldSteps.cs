namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>Walks over the things lying about (walking over one picks it up), up to three.</summary>
public sealed class PickUpStep : BotStep
{
    private Walker _walker = new Walker();
    private Node3D? _item;
    private int _taken;

    public PickUpStep()
        : base("pick things up", 90)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _item != null && GodotObject.IsInstanceValid(_item) && _item.IsInsideTree() ? _item.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _item = null;
        _taken = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_item == null || !GodotObject.IsInstanceValid(_item) || !_item.IsInsideTree())
        {
            if (_item != null)
            {
                _taken++;
            }

            _item = body.Nearest(body.GroundItems());
            _walker = new Walker();

            if (_item == null || _taken >= 3)
            {
                body.Stop();
                return _taken > 0 ? StepResult.Done : StepResult.Failed;
            }
        }

        // Near is below zero: it walks onto the thing until it is picked up and gone.
        if (_walker.Walk(body, _item.GlobalPosition, -1f, delta) == StepResult.Failed)
        {
            body.Unreachable.Add(_item.Name);
            return StepResult.Failed;
        }

        return StepResult.Running;
    }
}

/// <summary>Walks under the nearest drone and fires the EMP until it is down.</summary>
public sealed class HuntStep : BotStep
{
    private const float Under = 5f;
    private const double FireEvery = 1.5;

    private Walker _walker = new Walker();
    private Node3D? _drone;
    private double _fireIn;

    public HuntStep()
        : base("hunt a drone", 60)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _drone != null && GodotObject.IsInstanceValid(_drone) && _drone.IsInsideTree() ? _drone.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _drone = body.Nearest(body.LiveDrones());
        _walker = new Walker();
        _fireIn = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_drone == null)
        {
            return StepResult.Failed;
        }

        if (!GodotObject.IsInstanceValid(_drone) || !_drone.IsInsideTree() || ((Drones.Drone)_drone).Down)
        {
            body.Stop();
            GD.Print("Bot: the drone is down");
            return StepResult.Done;
        }

        // The ground under it, at the bot's own height: a drone over a building puts the
        // nearest point of the air on the roof, and the walk presses into the wall.
        Players.Player? me = body.Me;
        Vector3 under = me == null ? _drone.GlobalPosition : new Vector3(_drone.GlobalPosition.X, me.GlobalPosition.Y, _drone.GlobalPosition.Z);
        _walker.Walk(body, under, Under, delta);
        _fireIn -= delta;

        if (_fireIn <= 0 && body.DistanceTo(_drone.GlobalPosition) < Under * 2f)
        {
            _fireIn = FireEvery;
            BotBody.Press("emp");
        }

        return StepResult.Running;
    }
}

/// <summary>Walks to the parts of the map not discovered yet, nearest first.</summary>
public sealed class ExploreStep : BotStep
{
    private const float Near = 6f;

    private readonly List<Vector3> _unreachable = new List<Vector3>();
    private Walker _walker = new Walker();
    private Vector3? _spot;
    private int _reached;

    public ExploreStep()
        : base("explore", 90)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _spot;
    }

    public override void Begin(BotBody body)
    {
        _spot = null;
        _reached = 0;
        _unreachable.Clear();
    }

    private bool Unreachable(Vector3 spot)
    {
        foreach (Vector3 failed in _unreachable)
        {
            if (failed.DistanceTo(spot) < Rules.Maps.Discovery.CellSize)
            {
                return true;
            }
        }

        return false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_spot == null)
        {
            Vector3? best = null;

            foreach (Vector3 spot in body.Undiscovered())
            {
                if (!Unreachable(spot) && (best == null || body.DistanceTo(spot) < body.DistanceTo(best.Value)))
                {
                    best = spot;
                }
            }

            if (best == null)
            {
                return StepResult.Done;
            }

            _spot = best;
            _walker = new Walker();
        }

        StepResult walked = _walker.Walk(body, _spot.Value, Near, delta);

        if (walked == StepResult.Running)
        {
            return StepResult.Running;
        }

        // Out of reach (inside a building, past an edge): not tried again this step.
        if (walked == StepResult.Failed)
        {
            _unreachable.Add(_spot.Value);
        }

        _spot = null;
        _reached++;
        return _reached >= 4 ? StepResult.Done : StepResult.Running;
    }
}

/// <summary>
/// Goes up to another player and does one thing with them: add them as a friend, send
/// a message, invite them to a party, or give them something.
/// </summary>
public sealed class MeetStep : BotStep
{
    private const float Close = 3f;

    private Player? _other;
    private Walker _walker = new Walker();
    private int _stage;
    private double _wait;

    public MeetStep()
        : base("meet someone", 60)
    {
    }

    public override void Begin(BotBody body)
    {
        _other = null;
        _walker = new Walker();
        _stage = 0;
        _wait = 0;
        Player? me = body.Me;

        if (me == null)
        {
            return;
        }

        List<Player> others = new List<Player>();

        foreach (Node node in me.GetParent().GetChildren())
        {
            Player? other = node as Player;

            if (other != null && other != me)
            {
                others.Add(other);
            }
        }

        if (others.Count > 0)
        {
            _other = others[body.Random.Next(others.Count)];
        }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        // Gone through a door: the body leaves the tree before it is freed.
        if (_other == null || !GodotObject.IsInstanceValid(_other) || !_other.IsInsideTree() || body.Me == null)
        {
            return StepResult.Failed;
        }

        switch (_stage)
        {
            case 0:
                StepResult walked = _walker.Walk(body, _other.GlobalPosition, Close, delta);

                if (walked == StepResult.Done)
                {
                    _stage = 1;
                }

                return walked == StepResult.Failed ? StepResult.Failed : StepResult.Running;
            case 1:
                Camera3D? camera = body.Me.GetViewport().GetCamera3D();
                Vector3 chest = _other.GlobalPosition + new Vector3(0f, 1f, 0f);

                if (camera == null || camera.IsPositionBehind(chest))
                {
                    return StepResult.Failed;
                }

                GD.Print("Bot: clicking on " + _other.DisplayName);
                BotDriver.Click(camera.UnprojectPosition(chest));
                _stage = 2;
                _wait = 0.6;
                return StepResult.Running;
            case 2:
                _wait -= delta;

                if (_wait > 0)
                {
                    return StepResult.Running;
                }

                return Act(body);
            case 4:
                // The chat opened on the conversation; type into it.
                _wait -= delta;

                if (_wait > 0)
                {
                    return StepResult.Running;
                }

                BotDriver.Type("hi");
                return StepResult.Done;
            default:
                _wait -= delta;

                if (_wait > 0)
                {
                    return StepResult.Running;
                }

                Button? giveOne = body.Usable(GivePanel.GiveGroup);

                if (giveOne != null)
                {
                    GD.Print("Bot: clicking Give 1");
                    body.Click(giveOne);
                }

                return StepResult.Done;
        }
    }

    // One of the target frame's buttons, whichever it picks among those shown.
    private StepResult Act(BotBody body)
    {
        List<Button> choices = new List<Button>();
        string[] groups = { TargetFrame.FriendGroup, TargetFrame.MessageGroup, TargetFrame.InviteGroup, TargetFrame.GiveGroup };

        foreach (string group in groups)
        {
            Button? button = body.Usable(group);

            if (button != null)
            {
                choices.Add(button);
            }
        }

        if (choices.Count == 0)
        {
            return StepResult.Failed;
        }

        Button pick = choices[body.Random.Next(choices.Count)];
        GD.Print("Bot: clicking " + pick.Text);
        body.Click(pick);

        if (pick.IsInGroup(TargetFrame.MessageGroup))
        {
            _stage = 4;
            _wait = 0.5;
            return StepResult.Running;
        }

        if (pick.IsInGroup(TargetFrame.GiveGroup))
        {
            _stage = 3;
            _wait = 0.6;
            return StepResult.Running;
        }

        return StepResult.Done;
    }
}
