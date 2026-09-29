namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// Activities with whoever else is about: another player picked by a click on them, then
/// befriended, invited, given something or sent a line. They need no helper bot (bots.md
/// F6); in a soak the others are the other bots. With nobody else in town they stop at
/// once. Invites are answered by BotInviteAnswers, whatever the plan.
/// </summary>
public static class SocialActivities
{
    private static readonly string[] Lines = { "Hi there!", "Nice day for it.", "Seen any drones?", "Want to fix lights together?" };

    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            new BotActivity("befriend-someone", plan => Pick(plan)
                .Click<TargetFrame>("%Friend")
                .Wait(1)
                .Press("social")
                .UntilOpen("social")
                .Wait(1.5)
                .Press("social")
                .UntilClosed("social"))
                .Says("Let's be friends.", "Adding you."),

            // The other side may say no, or let the prompt go; either is fine.
            new BotActivity("invite-someone", plan => Pick(plan)
                .Click<TargetFrame>("%Invite")
                .Wait(8))
                .Says("Want to party up?", "Join me!"),

            new BotActivity("leave-party", plan => plan
                .InWorld()
                .StopIf("not in a party", body => body.Find<PartyPanel>() == null)
                .Click<PartyPanel>("%Leave")
                .Until("out of the party", body => body.Find<PartyPanel>() == null, 5))
                .Says("Going solo for a bit."),

            new BotActivity("give-someone-something", plan => Pick(plan)
                .Click<TargetFrame>("%Give")
                .WaitFor<GivePanel>()
                .ClickIfThere("Give 1", body => BotScreens.FirstButton(body.Find<GivePanel>()!, "Give 1"))
                .Wait(1)
                .StopIf("they left: the give panel went with them", body => body.Find<GivePanel>() == null)
                .Click<GivePanel>("%GiveMoney")
                .Wait(1)
                .Click<GivePanel>("%Close")
                .Until("the give panel closed", body => body.Find<GivePanel>() == null, 3))
                .Says("Here, have this.", "A gift for you."),

            new BotActivity("message-someone", plan => Pick(plan)
                .Click<TargetFrame>("%Message")
                .Until("the chat line has the keys", body => body.FocusedField() != null, 3)
                .Step(new TypeStep("a message", body => Lines[body.Random.Next(Lines.Length)]))),
        };
    }

    // To town, where the others mostly are, and a click on one of them.
    private static BotPlan Pick(BotPlan plan)
    {
        return plan
            .InWorld()
            .GoTo(ZoneIds.Town)
            .StopIf("nobody else here", body => body.OthersHere().Count == 0)
            .Step(new PickPlayerStep());
    }
}
