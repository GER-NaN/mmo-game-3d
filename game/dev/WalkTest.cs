namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Measures how smoothly this client draws a walk. The player walks steady circles
/// (through its own input source, so nothing else changes), and every few seconds this
/// prints the speed at which the body and the camera are seen to move, frame to frame:
/// a steady walk should read as one speed with almost no spread. Stutter shows as
/// spread and as big frame-to-frame jumps. With Watch set, the player stands still and
/// it measures the first other player in sight instead. Needs a real window.
/// </summary>
public partial class WalkTest : Node, IPlayerInput
{
    private const double ReportSeconds = 5;

    private readonly List<float> _bodySpeeds = new List<float>();
    private readonly List<float> _cameraSpeeds = new List<float>();
    private Player? _body;
    private Vector3 _lastBody;
    private Vector3 _lastCamera;
    private bool _started;
    private double _sinceReport;
    private Player? _watched;

    // Stand still and measure another player's walk instead of this one's.
    public bool Watch { get; set; }

    public float Turn
    {
        get { return Watch ? 0f : 0.3f; }
    }

    public float Forward
    {
        get { return Watch ? 0f : 1f; }
    }

    public float Strafe
    {
        get { return 0f; }
    }

    public bool TakeJump()
    {
        return false;
    }

    public override void _Process(double delta)
    {
        if (_body == null || !IsInstanceValid(_body))
        {
            _body = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;

            if (_body == null)
            {
                return;
            }

            _body.InputSource = this;
            _started = false;
        }

        Camera3D? camera = GetViewport().GetCamera3D();

        if (camera == null || delta <= 0)
        {
            return;
        }

        Player measured = _body;

        if (Watch)
        {
            if (_watched == null || !IsInstanceValid(_watched))
            {
                _watched = null;

                foreach (Node node in _body.GetParent().GetChildren())
                {
                    if (node is Player other && other != _body)
                    {
                        _watched = other;
                        _started = false;
                        break;
                    }
                }
            }

            if (_watched == null)
            {
                return;
            }

            measured = _watched;
        }

        Vector3 body = measured.GetGlobalTransformInterpolated().Origin;
        Vector3 eye = camera.GlobalPosition;

        if (_started)
        {
            _bodySpeeds.Add(Flat(body - _lastBody) / (float)delta);
            _cameraSpeeds.Add(Flat(eye - _lastCamera) / (float)delta);
        }

        _started = true;
        _lastBody = body;
        _lastCamera = eye;
        _sinceReport += delta;

        if (_sinceReport >= ReportSeconds)
        {
            _sinceReport = 0;
            GD.Print("Walk test: body " + Describe(_bodySpeeds) + "; camera " + Describe(_cameraSpeeds) + "; " + Engine.GetFramesPerSecond() + " fps");
            _bodySpeeds.Clear();
            _cameraSpeeds.Clear();
        }
    }

    private static float Flat(Vector3 moved)
    {
        return new Vector2(moved.X, moved.Z).Length();
    }

    // Mean and spread of the speed, and how often a frame's speed is far off the mean.
    private static string Describe(List<float> speeds)
    {
        if (speeds.Count == 0)
        {
            return "no samples";
        }

        double mean = 0;

        foreach (float speed in speeds)
        {
            mean += speed;
        }

        mean /= speeds.Count;
        double spread = 0;
        int offBeat = 0;

        foreach (float speed in speeds)
        {
            spread += (speed - mean) * (speed - mean);

            if (Math.Abs(speed - mean) > mean * 0.25)
            {
                offBeat++;
            }
        }

        spread = Math.Sqrt(spread / speeds.Count);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} m/s +/- {1:F2}, {2}% of frames 25% off", mean, spread, offBeat * 100 / speeds.Count);
    }
}
