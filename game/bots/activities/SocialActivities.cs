namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// Activities with whoever else is about: another player picked by a click on them, then
/// befriended, invited, given something, sent a line or ignored for a moment; and the
/// friends list, a friend removed or messaged from it. They need no helper bot (bots.md
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

            // The other side may say no, or let the prompt go; either is fine. Someone
            // already in the party has no Invite button.
            new BotActivity("invite-someone", plan => Pick(plan)
                .ClickIfThere("Invite", body => BotScreens.Named<Godot.Button>(body.Find<TargetFrame>(), "Invite"))
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

            // Ignored, seen in the list, and unignored again, so the bots do not end up
            // deaf to each other.
            new BotActivity("ignore-someone", plan =>
            {
                int ignored = 0;

                return Pick(plan)
                    .Click<TargetFrame>("%Ignore")
                    .Wait(1)
                    .Press("social")
                    .UntilOpen("social")
                    .Until("someone in the ignored list", body => Contact(body, "Unignore") != null, 5)
                    .Wait(1)
                    .Do("count the ignored", body => ignored = Contacts(body, "Unignore"))
                    .Click("Unignore", body => Contact(body, "Unignore"))
                    .Until("one fewer ignored", body => Contacts(body, "Unignore") < ignored, 5)
                    .Press("social")
                    .UntilClosed("social");
            }).Says("Shh."),

            new BotActivity("unfriend-someone", plan => FriendsList(plan)
                .StopIf("no friends to remove", body => Contact(body, "Remove") == null)
                .Click("Remove", body => Contact(body, "Remove"))
                .Wait(1)
                .Press("social")
                .UntilClosed("social")),

            new BotActivity("message-a-friend", plan => FriendsList(plan)
                .StopIf("no friend online", body => Contact(body, "Message") == null)
                .Click("Message", body => Contact(body, "Message"))
                .Until("the chat line has the keys", body => body.FocusedField() != null, 3)
                .Step(new TypeStep("a message", body => Lines[body.Random.Next(Lines.Length)]))),
        };
    }

    private static BotPlan FriendsList(BotPlan plan)
    {
        return plan
            .InWorld()
            .Press("social")
            .UntilOpen("social")
            .Wait(1);
    }

    // A button on a row of the friends panel, by its text.
    private static Godot.Button? Contact(BotBody body, string text)
    {
        SocialPanel? panel = body.Find<SocialPanel>();
        return panel == null ? null : BotScreens.FirstButton(panel, text);
    }

    // How many rows of the friends panel have this button.
    private static int Contacts(BotBody body, string text)
    {
        SocialPanel? panel = body.Find<SocialPanel>();
        return panel == null ? 0 : BotScreens.ButtonsWith(panel, text);
    }

    // To town, where the others mostly are, and a click on one of them. Just after a zone
    // loads, the others in it show a moment later.
    private static BotPlan Pick(BotPlan plan)
    {
        return plan
            .InWorld()
            .GoTo(ZoneIds.Town)
            .Wait(2)
            .StopIf("nobody else here", body => body.OthersHere().Count == 0)
            .Step(new PickPlayerStep());
    }
}
