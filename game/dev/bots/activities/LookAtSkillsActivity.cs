namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>A look at skills, then at friends.</summary>
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
        return new List<BotStep>
        {
            LookUi.OpenSkills(), new PauseStep(2.5), new CloseAllStep(),
            LookUi.OpenFriends(), new PauseStep(2.5), new CloseAllStep(),
        };
    }
}
