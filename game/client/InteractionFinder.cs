namespace MmoGame3d.Client;

using System;
using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Players;

/// <summary>
/// Finds the nearest thing in reach of this client's player, for the HUD prompt, and
/// asks the server to use it when F is pressed. The server checks reach again; this
/// only decides what to offer.
/// </summary>
public partial class InteractionFinder : Node
{
    private const double LookInterval = 0.1;

    private double _sinceLook;
    private Interactable? _nearest;
    private bool _wasPressed;

    // The prompt to show, or empty for none.
    public event Action<string>? PromptChanged;

    public event Action<string>? UseRequested;

    public bool Paused { get; set; }

    public override void _Process(double delta)
    {
        // Polled, and the press found as up-then-down here rather than by "just pressed",
        // so a press counts whatever order nodes run in. Not while a text field has the
        // keys.
        bool pressed = Input.IsActionPressed("interact");

        if (pressed && !_wasPressed && !Paused && _nearest != null && IsInstanceValid(_nearest) && GetViewport().GuiGetFocusOwner() == null)
        {
            UseRequested?.Invoke(_nearest.Name);
        }

        _wasPressed = pressed;

        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return;
        }

        _sinceLook = 0;
        Interactable? nearest = Paused ? null : FindNearest();
        string prompt = nearest == null ? "" : nearest.Prompt;

        _nearest = nearest;
        PromptChanged?.Invoke(prompt);
    }

    private Interactable? FindNearest()
    {
        Player? self = GetTree().GetFirstNodeInGroup(Player.LocalGroup) as Player;

        // The player is under the zone's "Players" node, so the zone is two up.
        Node? things = self?.GetParent()?.GetParent()?.GetNodeOrNull(Interactable.ParentName);

        if (self == null || things == null)
        {
            return null;
        }

        Interactable? best = null;
        float bestDistance = float.MaxValue;

        foreach (Node node in things.GetChildren())
        {
            Interactable? thing = node as Interactable;

            if (thing == null || !thing.IsInReach(self.GlobalPosition))
            {
                continue;
            }

            float distance = thing.GlobalPosition.DistanceTo(self.GlobalPosition);

            if (distance < bestDistance)
            {
                best = thing;
                bestDistance = distance;
            }
        }

        return best;
    }
}
