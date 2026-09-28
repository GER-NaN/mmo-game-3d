namespace MmoGame3d.Dev.Activities;
using MmoGame3d.BotJudging;

/// <summary>
/// A world event the bot was at, from its phone afterwards: Notifications must count it
/// (WorldEventCheck). A run that was cut short, or that never saw the swarm fly, is not
/// judged.
/// </summary>
public sealed class WorldEventJudge : BotActivityJudge
{
    private readonly CheckWorldEventsActivity _activity;

    public WorldEventJudge(CheckWorldEventsActivity activity)
    {
        _activity = activity;
    }

    public override string? After(BotBody body, BotEnd end)
    {
        if (end != BotEnd.Finished || !_activity.Going)
        {
            return null;
        }

        return WorldEventCheck.Judge(_activity.Line, _activity.SawItRunning, _activity.ReadAfter, _activity.PastAfter);
    }
}
