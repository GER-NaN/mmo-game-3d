namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

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
