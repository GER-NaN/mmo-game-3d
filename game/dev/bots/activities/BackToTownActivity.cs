namespace MmoGame3d.Dev.Activities;
using MmoGame3d.Rules.World;

/// <summary>
/// Back to Old Town from anywhere else: chosen often when away, since most things start
/// there, and the driver's way home when one thing is all it may do and that cannot start.
/// </summary>
public sealed class BackToTownActivity : TravelActivity
{
    public BackToTownActivity()
        : base(ZoneIds.Town, "go back to town", 20)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body) && body.ZoneId != ZoneIds.Town;
    }
}
