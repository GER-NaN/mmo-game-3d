namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Drones;
using MmoGame3d.Town;

/// <summary>
/// Old Town's cameras, live: a picture of the town the client already has loaded, seen
/// from one of four cameras high on the corners. Clicking a drone in the picture reports
/// it (the server checks and pays). Only in Old Town: elsewhere the town is not loaded,
/// so there is no feed.
/// </summary>
public partial class CctvView : SubViewportContainer
{
    // Placeholders: where the cameras hang, round the town's middle, and how near a click
    // must land to a drone on the picture, in pixels.
    private const float CameraHeight = 16f;
    private const float CameraOut = 38f;
    private const float ClickReach = 40f;
    private const double CycleSeconds = 10;

    private static readonly Vector2[] Corners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };

    private Camera3D _camera = null!;
    private Node3D? _town;
    private int _shown;
    private double _sinceCycle;

    // Dev scenarios find the view by this group.
    public const string Group = "cctv_view";

    // The drone's node name, when a click lands on one.
    public event Action<string>? DroneReported;

    // Which camera, 1 to 4, for the caption.
    public int CameraNumber
    {
        get { return _shown + 1; }
    }

    public bool HasFeed
    {
        get { return _town != null; }
    }

    public override void _Ready()
    {
        AddToGroup(Group);
        Stretch = true;
        MouseFilter = MouseFilterEnum.Stop;
        TownState? state = GetTree().GetFirstNodeInGroup(TownState.Group) as TownState;
        _town = state?.GetParent() as Node3D;

        if (_town == null)
        {
            return;
        }

        // No world of its own: it sees the one the client plays in.
        SubViewport viewport = new SubViewport { OwnWorld3D = false };
        AddChild(viewport);
        _camera = new Camera3D { Fov = 55f, Current = true };
        viewport.AddChild(_camera);
        Show(0);
    }

    public void Show(int camera)
    {
        if (_town == null)
        {
            return;
        }

        _shown = ((camera % Corners.Length) + Corners.Length) % Corners.Length;
        _sinceCycle = 0;
        Vector3 middle = _town.GlobalPosition + new Vector3(0f, 2f, 0f);
        Vector2 corner = Corners[_shown];
        _camera.GlobalPosition = _town.GlobalPosition + new Vector3(corner.X * CameraOut, CameraHeight, corner.Y * CameraOut);
        _camera.LookAt(middle);
    }

    public override void _Process(double delta)
    {
        _sinceCycle += delta;

        if (_town != null && _sinceCycle >= CycleSeconds)
        {
            Show(_shown + 1);
        }
    }

    // For dev scenarios, which click as a person does: where on the screen a flying
    // drone is in the picture, or null if none is.
    public Vector2? ScreenPointOfADrone()
    {
        if (_town == null)
        {
            return null;
        }

        foreach (Node node in _town.GetNode("Drones").GetChildren())
        {
            Drone? drone = node as Drone;

            if (drone == null || drone.Down || _camera.IsPositionBehind(drone.GlobalPosition))
            {
                continue;
            }

            Vector2 local = _camera.UnprojectPosition(drone.GlobalPosition);

            if (new Rect2(Vector2.Zero, Size).HasPoint(local))
            {
                return GlobalPosition + local;
            }
        }

        return null;
    }

    public override void _GuiInput(InputEvent @event)
    {
        InputEventMouseButton? button = @event as InputEventMouseButton;

        if (_town == null || button == null || !button.Pressed || button.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        foreach (Node node in _town.GetNode("Drones").GetChildren())
        {
            Drone? drone = node as Drone;

            if (drone == null || drone.Down || _camera.IsPositionBehind(drone.GlobalPosition))
            {
                continue;
            }

            if (_camera.UnprojectPosition(drone.GlobalPosition).DistanceTo(button.Position) <= ClickReach)
            {
                AcceptEvent();
                DroneReported?.Invoke(drone.Name);
                return;
            }
        }
    }
}
