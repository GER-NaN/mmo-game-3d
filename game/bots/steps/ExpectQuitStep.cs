namespace MmoGame3d.Bots;

/// <summary>
/// The last step when the plan ends by quitting the game (Quit on the main menu). The
/// bot's part is done, so it says so, then waits for the game to end the process. If
/// the stop file comes first, the game did not quit: the runner quits with exit code 1,
/// since the plan never finished.
/// </summary>
public class ExpectQuitStep : BotStep
{
    public ExpectQuitStep()
        : base("expect the game to quit", double.MaxValue)
    {
    }

    public override void Start(BotBody body)
    {
        body.Events.Write("done", "waiting for the game to quit");
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        return BotStepState.Running;
    }
}
