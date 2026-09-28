namespace MmoGame3d.Dev;
using Godot;
using MmoGame3d.Ui;

/// <summary>
/// Watches the Town cameras until a drone is in the picture and clicks it, which
/// reports it for the town's pay. The cameras change by themselves every few seconds.
/// </summary>
public sealed class CameraStep : BotStep
{
    private double _waited;

    public CameraStep()
        : base("report a drone", 45)
    {
    }

    public override void Begin(BotBody body)
    {
        _waited = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _waited += delta;
        CctvView? view = body.Me?.GetTree().GetFirstNodeInGroup(CctvView.Group) as CctvView;

        if (view == null || _waited < 1)
        {
            return StepResult.Running;
        }

        Vector2? at = view.ScreenPointOfADrone();

        if (at == null)
        {
            return StepResult.Running;
        }

        GD.Print("Bot: clicking a drone on camera " + view.CameraNumber);
        BotDriver.Click(at.Value);
        return StepResult.Done;
    }
}
