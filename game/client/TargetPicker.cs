namespace MmoGame3d.Client;

using System;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Left-click on another player's body selects them; a click anywhere else clears the
/// selection. The pick is a ray from the camera through the mouse, against the players
/// layer only, so buildings in front of a player do not stop the click at them.
/// </summary>
public partial class TargetPicker : Node
{
    private const float RayLength = 200f;

    public event Action<Player?>? Picked;

    public override void _UnhandledInput(InputEvent @event)
    {
        InputEventMouseButton? click = @event as InputEventMouseButton;

        if (click == null || click.ButtonIndex != MouseButton.Left || !click.Pressed)
        {
            return;
        }

        Camera3D? camera = GetViewport().GetCamera3D();

        if (camera == null)
        {
            return;
        }

        Vector3 from = camera.ProjectRayOrigin(click.Position);
        Vector3 to = from + (camera.ProjectRayNormal(click.Position) * RayLength);

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to, PhysicsLayers.Players);
        Godot.Collections.Dictionary hit = camera.GetWorld3D().DirectSpaceState.IntersectRay(query);

        Player? player = null;

        if (hit.Count > 0)
        {
            player = hit["collider"].As<Player>();
        }

        // Selecting yourself would only offer things you cannot do to yourself.
        if (player != null && player.IsInGroup(Player.LocalGroup))
        {
            player = null;
        }

        Picked?.Invoke(player);
    }
}
