namespace MmoGame3d.Bots;

using Godot;

/// <summary>
/// Presses a key once, down and up again a moment later, as a tap does. Up too soon
/// (the next frame, when frames outrun the physics ticks) and code that polls the key in
/// a physics tick never sees it "just pressed". The
/// key is an action's, or a key on the keyboard itself, for what the actions do not
/// cover (a key being bound to an action).
/// </summary>
public class KeyStep : BotStep
{
    private readonly string _action = "";
    private readonly Key _key = Key.None;
    private const double TapSeconds = 0.05;

    private ulong _pressedOnFrame;
    private double _down;

    public KeyStep(string action)
        : base("press " + action, DefaultTimeLimit)
    {
        _action = action;
    }

    public KeyStep(Key key)
        : base("press the " + key + " key", DefaultTimeLimit)
    {
        _key = key;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override void Start(BotBody body)
    {
        Send(body, true);
        _pressedOnFrame = Engine.GetProcessFrames();
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _down += delta;

        if (Engine.GetProcessFrames() == _pressedOnFrame || _down < TapSeconds)
        {
            return BotStepState.Running;
        }

        Send(body, false);
        return BotStepState.Done;
    }

    private void Send(BotBody body, bool pressed)
    {
        if (_key != Key.None)
        {
            body.RawKey(_key, pressed);
        }
        else
        {
            body.Key(_action, pressed);
        }
    }
}
