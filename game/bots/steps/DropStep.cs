namespace MmoGame3d.Bots;

using Godot;

/// <summary>
/// Loses the connection the hard way (bots.md R1, the dropper): ends its own client at
/// once, as a crash or a pulled cable would, so the server sees the player vanish mid-way.
/// It says so first ("dropping"), so the Overseer knows it was meant, and starts the bot
/// again as the same player; the keeper then logs back in to whatever was left.
/// </summary>
public class DropStep : BotStep
{
    public DropStep()
        : base("drop the connection", DefaultTimeLimit)
    {
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        body.Events.Write("dropping", "the client ends here, on purpose");
        OS.Kill(OS.GetProcessId());
        return BotStepState.Running;
    }
}
