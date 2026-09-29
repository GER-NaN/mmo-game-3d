namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Gets onto a fixed terminal in this zone: waits until one is free (one player at a
/// time), walks up to the nearest free one, uses it, and waits for its screen. If someone
/// else got there first ("Someone is using this terminal."), it waits and tries again, a
/// few times, as a player would.
/// </summary>
public class UseTerminalStep : BotStep
{
    private const int MaxTries = 8;
    private const double ScreenPatience = 5;

    // After a refusal, a moment before looking again, so the bots queued at one terminal
    // do not all rush the next.
    private const double BackOffMin = 2;
    private const double BackOffMax = 8;

    private ApproachStep<Terminal>? _approach;
    private int _tries;
    private int _stage;
    private double _stageTime;
    private int _noticesAt;
    private double _waitFirst;

    public UseTerminalStep()
        : base("use a free terminal", 180)
    {
    }

    public override BotIntent Intent
    {
        get { return _stage == 1 ? BotIntent.Walking : BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _stageTime += delta;

        switch (_stage)
        {
            // Waiting for a free terminal.
            case 0:
                if (_stageTime < _waitFirst || !AnyFree(body))
                {
                    return BotStepState.Running;
                }

                _approach = new ApproachStep<Terminal>("free", Free);
                _approach.Start(body);
                Next(1);
                return BotStepState.Running;

            // Walking up to it.
            case 1:
                BotStepState walked = _approach!.Tick(body, delta);

                if (walked == BotStepState.Failed)
                {
                    return Fail(_approach.FailReason);
                }

                if (walked == BotStepState.Running)
                {
                    return BotStepState.Running;
                }

                _approach.End(body);

                // Taken while it walked up: back to waiting, without pressing.
                Terminal? reached = body.LastApproached as Terminal;

                if (reached != null && !Free(reached))
                {
                    Retry(body, "taken while it walked up");
                    return _tries >= MaxTries ? Fail("could not get onto a terminal in " + MaxTries + " tries") : BotStepState.Running;
                }

                _noticesAt = body.View?.NoticeCount ?? 0;
                body.Key("interact", true);
                Next(2);
                return BotStepState.Running;

            // The key up on the next frame, then the screen.
            default:
                if (_stage == 2)
                {
                    body.Key("interact", false);
                    Next(3);
                }

                if (body.Find<TerminalScreen>() != null)
                {
                    return BotStepState.Done;
                }

                bool refused = false;

                foreach (string notice in body.NoticesSince(_noticesAt))
                {
                    refused = refused || notice.Contains("Someone is using");
                }

                if (!refused && _stageTime < ScreenPatience)
                {
                    return BotStepState.Running;
                }

                Retry(body, refused ? "someone got there first" : "no screen");
                return _tries >= MaxTries ? Fail("could not get onto a terminal in " + MaxTries + " tries") : BotStepState.Running;
        }
    }

    public override void End(BotBody body)
    {
        _approach?.End(body);
    }

    public static bool Free(Terminal terminal)
    {
        return terminal.Enabled && terminal.UsedBy.Length == 0;
    }

    public static bool AnyFree(BotBody body)
    {
        Node? things = body.Zone?.GetNodeOrNull(Interactable.ParentName);

        if (things == null)
        {
            return false;
        }

        foreach (Node child in things.GetChildren())
        {
            Terminal? terminal = child as Terminal;

            if (terminal != null && Free(terminal))
            {
                return true;
            }
        }

        return false;
    }

    private void Retry(BotBody body, string why)
    {
        _tries++;
        _waitFirst = BackOffMin + (body.Random.NextDouble() * (BackOffMax - BackOffMin));
        body.Events.Write("terminal", why + "; trying again");
        Next(0);
    }

    private void Next(int stage)
    {
        _stage = stage;
        _stageTime = 0;
    }
}
