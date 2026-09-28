namespace MmoGame3d.Dev.Screens;

using Godot;
using MmoGame3d.Ui;

/// <summary>
/// The college's people (CollegePanel): the registrar's careers and Class, the professor's
/// rank.
/// </summary>
public static class CollegeUi
{
    // Whatever there is to do: the Class, a career, a rank.
    public static BotStep DoWhatIsThere()
    {
        double read = 0;
        return new DoStep("work the college panel", 6, (b, d) =>
        {
            read += d;

            if (read < 1)
            {
                return StepResult.Running;
            }

            Button? button = b.Usable(CollegePanel.ClassGroup) ?? b.Usable(CollegePanel.EnrollGroup) ?? b.Usable(CollegePanel.RankUpGroup);

            if (button == null)
            {
                return StepResult.Done;
            }

            return b.TryClick(button) ? StepResult.Done : StepResult.Running;
        });
    }
}
