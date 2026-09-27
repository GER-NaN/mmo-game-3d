namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Rules.Skills;
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

/// <summary>
/// Enrolling in a career with the college's registrar, or changing to it from another (the
/// same button, worded "Change to"). A career the bot is not ready for has no button to
/// press: the enroll step runs out, and the activity fails.
/// </summary>
public sealed class EnrollActivity : StepsActivity
{
    private readonly CareerId _career;
    private readonly List<BotFact> _gives;

    public EnrollActivity(CareerId career)
        : base("enroll as " + CareerCatalog.Get(career).Name, 0)
    {
        _career = career;
        _gives = new List<BotFact> { BotFact.Career(career) };
    }

    public override string Zone
    {
        get { return ZoneIds.College; }
    }

    public override IReadOnlyList<BotFact> Gives
    {
        get { return _gives; }
    }

    // Not the career it has: there is no button for that.
    public override bool CanStart(BotBody body)
    {
        return body.Career != (int)_career;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        string name = CareerCatalog.Get(_career).Name;
        return new BotPlan()
            .WalkTo("Interactables/Registrar")
            .Use("Talk to", b => b.IsOpen<CollegePanel>())
            .Pause(1)
            .Do("enroll as " + name, 6, (b, d) =>
            {
                foreach (Node node in b.Tree.GetNodesInGroup(CollegePanel.EnrollGroup))
                {
                    Button? button = node as Button;

                    if (button == null || !button.IsVisibleInTree() || !button.Text.Contains(name))
                    {
                        continue;
                    }

                    // Off: the skills it asks for are not there yet. A refusal, not a fault.
                    if (button.Disabled)
                    {
                        Why = "not ready to be a " + name + " (its button is off)";
                        return StepResult.Failed;
                    }

                    if (!b.TryClick(button))
                    {
                        return StepResult.Running;
                    }

                    GD.Print("Bot: clicked " + button.Text);
                    return StepResult.Done;
                }

                return StepResult.Running;
            })
            .Pause(1.5)
            .Close()
            .Steps;
    }
}
