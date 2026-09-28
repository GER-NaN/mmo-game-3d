namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>
/// Another of the account's characters: the game menu, Leave, then Play and the other
/// character's card, or a second one made when there is none. The bot ends at Leave;
/// BotKeeper, which outlives the world, does the menus and judges the switch.
/// </summary>
public sealed class SwitchCharacterActivity : StepsActivity
{
    public SwitchCharacterActivity()
        : base("switch characters", 1)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Me != null && body.Me.GetTree().GetFirstNodeInGroup(BotKeeper.Group) != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep>
        {
            new CloseAllStep(),
            WardrobeUi.OpenMenu(),
            new PauseStep(0.8),
            new DoStep("tell the keeper", 1, (b, d) =>
            {
                BotKeeper? keeper = b.Me?.GetTree().GetFirstNodeInGroup(BotKeeper.Group) as BotKeeper;

                if (keeper == null || b.Me == null)
                {
                    return StepResult.Failed;
                }

                keeper.Switch(b.Me.DisplayName);
                return StepResult.Done;
            }),
            WardrobeUi.Leave(),
        };
    }
}
