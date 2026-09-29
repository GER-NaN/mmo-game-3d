namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Rules.World;
using MmoGame3d.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Activities at a terminal (a fixed one in town, or the phone): every app tried in turn,
/// the code cracker played through, a run of Agent Defense, a line in the terminal's chat,
/// one's own Whois page, the notifications of world events. Each leaves the terminal at
/// the end.
/// </summary>
public static class TerminalActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            AtTerminal("terminal-tour", plan => plan
                .ClickEach("the apps", body => body.Find<TerminalScreen>()?.GetNodeOrNull("%Apps"), button => true))
                .Says("Let's see what this terminal does.", "So many apps."),

            AtTerminal("code-cracker", plan => plan
                .Click("the Code cracker app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Code cracker"))
                .Click("New code", body => BotScreens.FirstButton(body.Find<TerminalScreen>()!, "New code"))
                .Step(new CrackStep()))
                .Says("I'll crack this.", "Four digits, easy."),

            AtTerminal("agent-defense", plan => plan
                .Click("the Defense Objectives app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Defense Objectives"))
                .Click("Start a run", body => BotScreens.FirstButton(body.Find<TerminalScreen>()!, "Start a run"))
                .Step(new DefenseStep()))
                .Says("Defending the grid!", "Here comes the AI."),

            AtTerminal("terminal-chat", plan => plan
                .Click("the Chat app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Chat"))
                .Click("the chat line", body => BotScreens.FirstOf<Godot.LineEdit>(body.Find<TerminalScreen>()))
                .Type("hello from a terminal")),

            AtTerminal("whois-mine", plan => plan
                .Click("the Whois app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Whois"))
                .Click("My page", body => BotScreens.FirstButton(body.Find<TerminalScreen>()!, "My page"))
                .Wait(1)),

            new BotActivity("check-world-events", plan => plan
                .InWorld()
                .Wait(1)
                .Need(BotFacts.PhoneEquipped)
                .Press("phone")
                .WaitFor<TerminalScreen>()
                .Click("the Notifications app", body => BotScreens.TerminalApp(body.Find<TerminalScreen>(), "Notifications"))
                .Wait(2)
                .Press("ui_cancel")
                .UntilClosed("terminal"))
                .Says("Anything happening out there?"),
        };
    }

    // An activity at a fixed terminal in town: waits for one to be free (one player at a
    // time), walks up to the nearest free one, uses it, does the steps, leaves.
    private static BotActivity AtTerminal(string name, System.Func<BotPlan, BotPlan> steps)
    {
        return new BotActivity(name, plan =>
        {
            plan.InWorld()
                .GoTo(ZoneIds.Town)
                .Until("a terminal is free", AnyFree, 90)
                .Use<Terminal>("free", Free)
                .WaitFor<TerminalScreen>();

            return steps(plan)
                .Wait(0.5)
                .Press("ui_cancel")
                .UntilClosed("terminal");
        });
    }

    private static bool Free(Terminal terminal)
    {
        return terminal.Enabled && terminal.UsedBy.Length == 0;
    }

    private static bool AnyFree(BotBody body)
    {
        Godot.Node? things = body.Zone?.GetNodeOrNull(Interact.Interactable.ParentName);

        if (things == null)
        {
            return false;
        }

        foreach (Godot.Node child in things.GetChildren())
        {
            Terminal? terminal = child as Terminal;

            if (terminal != null && Free(terminal))
            {
                return true;
            }
        }

        return false;
    }
}
