namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>
/// A look at skills, then at friends, and on half the looks one friend removed or one
/// player unignored, so ignores met in passing do not pile up.
/// </summary>
public sealed class LookAtSkillsActivity : StepsActivity
{
    public LookAtSkillsActivity()
        : base("look at skills and friends", 1)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return BotWhere.InWorld(body);
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        List<BotStep> steps = new List<BotStep>
        {
            LookUi.OpenSkills(), new PauseStep(2.5), new CloseAllStep(),
            LookUi.OpenFriends(), new PauseStep(2.5),
        };

        if (body.Random.NextDouble() < 0.5)
        {
            steps.Add(ScreenSteps.ClickAny("remove or unignore someone", Ui.SocialPanel.RemoveGroup, true));
            steps.Add(new PauseStep(1));
        }

        steps.Add(new CloseAllStep());
        return steps;
    }
}
