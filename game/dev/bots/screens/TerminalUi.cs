namespace MmoGame3d.Dev.Screens;

using System.Collections.Generic;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// A terminal's screen, on a public terminal or on the phone (TerminalScreen): going
/// online, its apps, the Whois page, Agent Defense, going offline.
/// </summary>
public static class TerminalUi
{
    // At a terminal in reach: F while the prompt offers to go online.
    public static BotStep GoOnline()
    {
        return new UseStep("Go Online", b => b.IsOnline);
    }

    // The phone's key once, then online within a few seconds, or the phone is dead or not
    // worn.
    public static BotStep PhoneOut()
    {
        bool pressed = false;
        return new DoStep("take the phone out", 4, (b, d) =>
        {
            if (b.IsOnline)
            {
                return StepResult.Done;
            }

            if (!pressed)
            {
                pressed = true;
                b.Stop();
                BotBody.Press("phone");
            }

            return StepResult.Running;
        });
    }

    public static BotStep OpenApp(string appId, bool optional = false)
    {
        return ScreenSteps.ClickAny("open " + appId, TerminalScreen.AppGroupPrefix + appId, optional);
    }

    // The Notifications rows as shown: the current ones, or the past ones, newest first.
    public static List<string> EventRows(BotBody body, bool current)
    {
        List<string> rows = new List<string>();
        Godot.SceneTree? tree = body.Me?.GetTree();

        if (tree == null)
        {
            return rows;
        }

        // The app lists current rows first, then a "Past" heading, then past rows.
        foreach (Godot.Node node in tree.GetNodesInGroup(TerminalScreen.EventRowGroup))
        {
            Godot.Label? label = node as Godot.Label;

            if (label != null && !label.IsQueuedForDeletion() && label.HasMeta(TerminalScreen.EventRowPastMeta) != current)
            {
                rows.Add(label.Text);
            }
        }

        return rows;
    }

    // A few apps at random, sometimes the code cracker or the repair job, then offline.
    public static BotStep Browse()
    {
        return new OnlineStep();
    }

    // Defense Objectives: a run started and every cue played.
    public static List<BotStep> PlayDefense()
    {
        return new List<BotStep>
        {
            OpenApp(TerminalApps.Defense),
            new PauseStep(0.8),
            ScreenSteps.ClickAny("start a run", TerminalScreen.DefenseStartGroup),
            new DefenseStep(),
        };
    }

    // Town cameras: a drone clicked in the pictures, if one is in sight.
    public static List<BotStep> WatchCameras()
    {
        return new List<BotStep>
        {
            OpenApp(TerminalApps.TownCameras),
            new CameraStep(),
        };
    }

    // Whois: the bot's own page, its plan typed, Show Skills flipped.
    public static List<BotStep> EditWhois(string plan)
    {
        return new BotPlan()
            .Step(OpenApp(TerminalApps.Whois))
            .Pause(1)
            .Click(TerminalScreen.WhoisMineGroup)
            .Pause(1)
            .Type(TerminalScreen.WhoisPlanGroup, plan)
            .Pause(1)
            .Click(TerminalScreen.WhoisShowSkillsGroup, "", true)
            .Steps;
    }
}
