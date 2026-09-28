namespace MmoGame3d.Dev;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Ui;

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
