namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Drones;
using MmoGame3d.Town;

/// <summary>
/// Old Town's cameras, live: a picture of the town the client already has loaded, seen
/// from one of the security cameras placed in it (CameraMount), low ones on building
/// corners and high ones on roofs and lamp posts. A broken camera shows no signal until
/// someone fixes it. Clicking a drone in the picture reports it (the server checks and
/// pays). Only in Old Town: elsewhere the town is not loaded, so there is no feed.
/// </summary>
public partial class CctvView : SubViewportContainer
{
    // Placeholders: how near a click must land to a drone on the picture, in pixels, and
    // how long each camera shows before the next.
    private const float ClickReach = 40f;
    private const double CycleSeconds = 10;

    private readonly List<CameraMount> _cameras = new List<CameraMount>();
    private Camera3D _camera = null!;
    private Control _noSignal = null!;
    private Node3D? _town;
    private int _shown;
    private double _sinceCycle;

    // The drone's node name, when a click lands on one.
    public event Action<string>? DroneReported;

    // Which camera, from 1, for the caption.
    public int CameraNumber
    {
        get { return _shown + 1; }
    }

    public int CameraCount
    {
        get { return _cameras.Count; }
    }

    public bool ShownWorks
    {
        get { return _cameras.Count > 0 && _cameras[_shown].Working; }
    }

    public bool HasFeed
    {
        get { return _town != null && _cameras.Count > 0; }
    }

    public override void _Ready()
    {
        Stretch = true;
        MouseFilter = MouseFilterEnum.Stop;
        TownState? state = GetTree().GetFirstNodeInGroup(TownState.Group) as TownState;
        _town = state?.GetParent() as Node3D;

        if (_town == null)
        {
            return;
        }

        foreach (Node node in GetTree().GetNodesInGroup(CameraMount.Group))
        {
            CameraMount? mount = node as CameraMount;

            if (mount != null && _town.IsAncestorOf(mount))
            {
                _cameras.Add(mount);
            }
        }

        _cameras.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        // No world of its own: it sees the one the client plays in. The no-signal card is
        // drawn inside the picture.
        SubViewport viewport = new SubViewport { OwnWorld3D = false };
        AddChild(viewport);
        // The near plane past the camera's own body, which the view point sits inside.
        _camera = new Camera3D { Fov = 55f, Current = true, Near = 0.4f };
        viewport.AddChild(_camera);
        _noSignal = new ColorRect { Color = new Color(0.05f, 0.05f, 0.06f), Visible = false };
        _noSignal.SetAnchorsPreset(LayoutPreset.FullRect);
        Label label = new Label { Text = "NO SIGNAL", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        label.SetAnchorsPreset(LayoutPreset.FullRect);
        _noSignal.AddChild(label);
        viewport.AddChild(_noSignal);
        Show(0);
    }

    public void Show(int camera)
    {
        if (!HasFeed)
        {
            return;
        }

        _shown = ((camera % _cameras.Count) + _cameras.Count) % _cameras.Count;
        _sinceCycle = 0;
        CameraMount mount = _cameras[_shown];
        _camera.GlobalPosition = mount.GlobalPosition;
        _camera.LookAt(_town!.ToGlobal(mount.Target));
    }

    public override void _Process(double delta)
    {
        if (!HasFeed)
        {
            return;
        }

        _sinceCycle += delta;
        _noSignal.Visible = !ShownWorks;

        if (_sinceCycle >= CycleSeconds)
        {
            Show(_shown + 1);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        InputEventMouseButton? button = @event as InputEventMouseButton;

        if (_town == null || !ShownWorks || button == null || !button.Pressed || button.ButtonIndex != MouseButton.Left)
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
