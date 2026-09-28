namespace MmoGame3d.Dev.Screens;

using Godot;
using MmoGame3d.Ui;

/// <summary>The shop's workbench (WorkbenchPanel): a phone's battery out, and one in.</summary>
public static class WorkbenchUi
{
    public static BotStep TakeBatteryOut()
    {
        return ScreenSteps.ClickAny("take the battery out", WorkbenchPanel.RemoveGroup, true);
    }

    // The list puts the fullest first. After a battery comes out the bench asks the
    // server again, so its buttons come a moment later: waited for, then none means
    // there is nothing to put in.
    public static BotStep PutFullestIn()
    {
        double waited = 0;
        return new DoStep("put the fullest battery in", 5, (b, d) =>
        {
            Button? first = b.Usable(WorkbenchPanel.InsertGroup);

            if (first == null)
            {
                // The swap starts only with a spare battery, so no button is a failure,
                // not a swap done.
                waited += d;
                return waited < ButtonsWithin ? StepResult.Running : StepResult.Failed;
            }

            return b.TryClick(first) ? StepResult.Done : StepResult.Running;
        });
    }

    private const double ButtonsWithin = 3;

    public static BotStep PutAnyIn()
    {
        return ScreenSteps.ClickAny("put a battery in", WorkbenchPanel.InsertGroup, true);
    }
}
