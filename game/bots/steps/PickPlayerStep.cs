namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Ui;
using MmoGame3d.Zones;

/// <summary>
/// Picks another player in this zone as the target, as a player does: walks up to them
/// and clicks on their body until the target frame shows. Someone who leaves the zone on
/// the way is swapped for another; with nobody left, it fails. A player behind the camera
/// is walked closer, which turns the camera to them.
/// </summary>
public class PickPlayerStep : BotStep
{
    // Placeholders: close enough to click on, and closer still when they are not on screen.
    private const float ClickRange = 5f;
    private const float CloseRange = 2f;
    private const float RepathMoved = 2f;
    private const double ClickEvery = 0.6;
    private const int MaxClicks = 8;
    private const int ClicksBeforeCloser = 3;

    // Where on the body to click: its middle, above the feet.
    private static readonly Vector3 Middle = new Vector3(0f, 1.1f, 0f);

    private Player? _target;
    private Vector3 _pathTo;
    private bool _walking;
    private float _range = ClickRange;
    private double _sinceClick = ClickEvery;
    private int _clicks;

    public PickPlayerStep()
        : base("pick another player", 60)
    {
    }

    public override BotIntent Intent
    {
        get { return _walking ? BotIntent.Walking : BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        TargetFrame? frame = body.Find<TargetFrame>();

        // Whoever the click hit, which may be someone standing nearer than the one meant.
        if (_clicks > 0 && frame != null)
        {
            body.Events.Write("picked", frame.GetNode<Label>("%Name").Text);
            return BotStepState.Done;
        }

        Player? me = body.Player;
        Zone? zone = body.Zone;

        if (me == null || zone == null)
        {
            return BotStepState.Running;
        }

        if (_target == null || !GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree())
        {
            List<Player> others = body.OthersHere();

            if (others.Count == 0)
            {
                return Fail("nobody else here");
            }

            _target = others[body.Random.Next(others.Count)];
            _walking = false;
            _range = ClickRange;
            body.Events.Write("approaching", _target.DisplayName);
        }

        Vector3 there = _target.GlobalPosition;

        if (me.GlobalPosition.DistanceTo(there) > _range)
        {
            if (!_walking || _pathTo.DistanceTo(there) > RepathMoved)
            {
                body.Navigator.Go(zone, there, false);
                _pathTo = there;
                _walking = true;
            }

            body.Navigator.Tick(body, delta);
            return BotStepState.Running;
        }

        if (_walking)
        {
            body.Navigator.Stop(body);
            _walking = false;
        }

        _sinceClick += delta;

        if (_sinceClick < ClickEvery)
        {
            return BotStepState.Running;
        }

        _sinceClick = 0;

        if (_clicks >= MaxClicks)
        {
            Control? over = me.GetViewport().GuiGetHoveredControl();
            return Fail("clicked " + _target.DisplayName + " " + MaxClicks + " times and no target frame showed; the mouse was over " + (over == null ? "the world" : over.GetPath().ToString()));
        }

        _clicks++;
        Camera3D? camera = me.GetViewport().GetCamera3D();
        Vector3 point = there + Middle;

        if (camera == null || camera.IsPositionBehind(point) || !camera.GetViewport().GetVisibleRect().HasPoint(camera.UnprojectPosition(point)))
        {
            _range = CloseRange;
            return BotStepState.Running;
        }

        body.ClickScreen(camera.GetViewport(), camera.UnprojectPosition(point));

        // Clicks that missed may have landed on a panel over them (the chat, the party):
        // closer, they fill the middle of the window.
        if (_clicks >= ClicksBeforeCloser)
        {
            _range = CloseRange;
        }

        return BotStepState.Running;
    }

    public override void End(BotBody body)
    {
        body.Navigator.Stop(body);
    }
}
