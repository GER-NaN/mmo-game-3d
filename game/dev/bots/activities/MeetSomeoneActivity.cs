namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;

/// <summary>Up to another player, and one thing with them: a friend, a message, a party invite or a gift.</summary>
public sealed class MeetSomeoneActivity : StepsActivity
{
    public MeetSomeoneActivity()
        : base("meet someone", 4)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { new MeetStep(), new PauseStep(1), new CloseAllStep() };
    }
}
