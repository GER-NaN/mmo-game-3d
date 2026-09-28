namespace MmoGame3d.Dev;

using System;
using Godot;

/// <summary>
/// Uses the thing in reach: taps F while the prompt says what it expects, until the
/// result shows (a panel opens, the zone changes, the prompt changes).
/// </summary>
public sealed class UseStep : BotStep
{
    private const double Retry = 2;

    private readonly string _prompt;
    private readonly Func<BotBody, bool> _until;
    private readonly bool _movesZone;
    private readonly bool _once;
    private double _nextTap;
    private double _sinceTap = -1;
    private string _seen = "";

    // once: one tap, done a moment later, for a use whose result the bot cannot see.
    public UseStep(string prompt, Func<BotBody, bool> until, double limit = 8, bool movesZone = false, bool once = false)
        : base("use \"" + prompt + "\"", limit)
    {
        _prompt = prompt;
        _until = until;
        _movesZone = movesZone;
        _once = once;
    }

    public override bool MovesZone
    {
        get { return _movesZone; }
    }

    public override void Begin(BotBody body)
    {
        _nextTap = 0;
        _sinceTap = -1;
        _seen = "";
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_until(body))
        {
            return StepResult.Done;
        }

        // What the prompt says instead, for the log when the use never comes: "In use
        // by", another thing in reach, nothing.
        if (body.Prompt != _seen)
        {
            _seen = body.Prompt;

            if (!_seen.Contains(_prompt))
            {
                GD.Print("Bot: waiting to " + _prompt + "; the prompt says \"" + _seen + "\"");
            }
        }

        if (_sinceTap >= 0)
        {
            _sinceTap += delta;

            if (_once && _sinceTap > 1.5)
            {
                return StepResult.Done;
            }
        }

        _nextTap -= delta;

        if (_nextTap <= 0 && body.Prompt.Contains(_prompt) && !(_once && _sinceTap >= 0))
        {
            GD.Print("Bot: pressing F at \"" + body.Prompt + "\"");
            body.Interact();
            _nextTap = Retry;
            _sinceTap = 0;
        }

        return StepResult.Running;
    }
}
