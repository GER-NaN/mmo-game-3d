namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// A visit to the college: the registrar and the professor, and whatever each has to do
/// (the Class, a career, a rank).
/// </summary>
public sealed class VisitCollegeActivity : StepsActivity
{
    public VisitCollegeActivity()
        : base("visit the college", 3)
    {
    }

    public override string Zone
    {
        get { return ZoneIds.College; }
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new BotPlan()
            .Wander(4 + (body.Random.NextDouble() * 4))
            .WalkTo("Interactables/Registrar")
            .Use("Talk to", b => b.IsOpen<CollegePanel>())
            .Step(CollegeUi.DoWhatIsThere())
            .Close()
            .WalkTo("Interactables/Professor")
            .Use("Talk to", b => b.IsOpen<CollegePanel>())
            .Step(CollegeUi.DoWhatIsThere())
            .Close()
            .Step(ChatUi.SayInPassing(body.Random))
            .Steps;
    }
}
