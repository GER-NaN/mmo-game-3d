namespace MmoGame3d.Ui;

using System;
using Godot;

// Covers the screen from Play on the character screen until the world is ready, so the
// wait for the server and the zone's load is not a frozen character screen.
public partial class LoadingScreen : Control
{
    // Past this, it goes anyway: better a half-loaded world than a screen that never lifts.
    private const double GiveUpSeconds = 15;

    // Frames the world has been ready before it lifts, so the first frames drawn are the
    // world's and not a hitch.
    private const int ReadyFrames = 3;

    public event Action? Finished;

    private Func<bool> _ready = () => false;
    private double _time;
    private int _readyFor;

    public void Start(string detail, Func<bool> ready)
    {
        GetNode<Label>("%Detail").Text = detail;
        _ready = ready;
    }

    public override void _Process(double delta)
    {
        _time += delta;

        // The HUD and the chat are added after it, as the world comes in; it stays on top.
        MoveToFront();
        GetNode<Label>("%Title").Text = "Loading" + new string('.', (int)(_time * 3) % 4);
        _readyFor = _ready() ? _readyFor + 1 : 0;

        if (_readyFor >= ReadyFrames || _time > GiveUpSeconds)
        {
            SetProcess(false);
            GD.Print("Loading screen: lifted after " + _time.ToString("0.0") + " s" + (_readyFor >= ReadyFrames ? "" : ", given up"));
            Finished?.Invoke();
        }
    }
}
