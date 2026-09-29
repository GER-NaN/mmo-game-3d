namespace MmoGame3d.Bots;

using Godot;

/// <summary>
/// Presses a key once, down on one frame and up on a later one, as a tap does. Down and
/// up in one frame would never be seen as "just pressed" by code that polls the key. The
/// key is an action's, or a key on the keyboard itself, for what the actions do not
/// cover (a key being bound to an action).
/// </summary>
public class KeyStep : BotStep
{
    private readonly string _action = "";
    private readonly Key _key = Key.None;
    private ulong _pressedOnFrame;

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
        if (Engine.GetProcessFrames() == _pressedOnFrame)
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
