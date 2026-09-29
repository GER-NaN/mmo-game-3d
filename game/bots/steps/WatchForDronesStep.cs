namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Drones;
using MmoGame3d.Ui;
using MmoGame3d.Zones;

/// <summary>
/// Watches the town cameras (CctvView, in a terminal's Town cameras app) and clicks each
/// drone that shows in the picture, as a player spotting it would, until the town has paid
/// for enough reports. It reads where the drones are and where the camera looks; the
/// report itself is a real click on the picture, which the view checks as it would a
/// player's. The server pays once per drone and player, whatever the activity, so a drone
/// turned down is skipped and not counted.
/// </summary>
public class WatchForDronesStep : BotStep
{
    private const double LookInterval = 0.25;

    // Clear of the picture's edge, so a drone half out of frame is not clicked.
    private const float EdgeMargin = 10f;

    private readonly int _wanted;
    private readonly HashSet<string> _reported = new HashSet<string>();
    private double _sinceLook = LookInterval;
    private int _noticesAt;
    private int _paid;

    public WatchForDronesStep(int wanted, double timeLimit)
        : base("watch the cameras for " + wanted + " drone" + (wanted == 1 ? "" : "s"), timeLimit)
    {
        _wanted = wanted;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override void Start(BotBody body)
    {
        _noticesAt = body.View?.NoticeCount ?? 0;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        foreach (string notice in body.NoticesSince(_noticesAt))
        {
            if (notice.StartsWith("Cameras: drone reported"))
            {
                _paid++;
            }
        }

        _noticesAt = body.View?.NoticeCount ?? _noticesAt;

        if (_paid >= _wanted)
        {
            return BotStepState.Done;
        }

        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;
        CctvView? view = body.Find<CctvView>();
        Zone? zone = body.Zone;

        if (view == null || zone == null)
        {
            return BotStepState.Running;
        }

        if (!view.HasFeed)
        {
            return Fail("no camera feed in " + zone.ZoneId);
        }

        Camera3D? camera = CameraOf(view);
        Node? drones = zone.GetNodeOrNull("Drones");

        if (!view.ShownWorks || camera == null || drones == null)
        {
            return BotStepState.Running;
        }

        Rect2 picture = new Rect2(Vector2.One * EdgeMargin, view.Size - (Vector2.One * EdgeMargin * 2));

        foreach (Node node in drones.GetChildren())
        {
            Drone? drone = node as Drone;

            if (drone == null || drone.Down || _reported.Contains(drone.Name) || camera.IsPositionBehind(drone.GlobalPosition))
            {
                continue;
            }

            Vector2 spot = camera.UnprojectPosition(drone.GlobalPosition);

            if (!picture.HasPoint(spot))
            {
                continue;
            }

            body.ClickAt(view, spot);
            _reported.Add(drone.Name);
            body.Events.Write("reported", drone.Name + " on camera " + view.CameraNumber);
            break;
        }

        return BotStepState.Running;
    }

    // The view draws through a SubViewport of its own, with the camera inside it.
    private static Camera3D? CameraOf(CctvView view)
    {
        foreach (Node child in view.GetChildren())
        {
            SubViewport? picture = child as SubViewport;

            if (picture == null)
            {
                continue;
            }

            foreach (Node inside in picture.GetChildren())
            {
                Camera3D? camera = inside as Camera3D;

                if (camera != null)
                {
                    return camera;
                }
            }
        }

        return null;
    }
}
