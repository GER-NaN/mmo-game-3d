namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

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

/// <summary>Out of the party it is in.</summary>
public sealed class LeavePartyActivity : StepsActivity
{
    public LeavePartyActivity()
        : base("leave the party", 2)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return PartyUi.InParty(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { PartyUi.Leave() };
    }
}
