namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Players;
using MmoGame3d.Zones;

/// <summary>
/// The bot's senses and hands, shared by every step. Senses read the client: its
/// screens, its player, its zone, its view of the player's state. Hands act only as a
/// player can: keys pressed and held through the input system, and clicks at a
/// control's centre through the viewport (bots.md F2, T1).
/// </summary>
public class BotBody
{
    private readonly Node _node;
    private readonly Action<List<BotStep>> _insertNext;
    private readonly HashSet<string> _held = new HashSet<string>();

    public BotBody(Node node, BotEventLog events, Action<List<BotStep>> insertNext)
    {
        _node = node;
        Events = events;
        _insertNext = insertNext;
    }

    public BotEventLog Events { get; }

    public BotNavigator Navigator { get; } = new BotNavigator();

    // The local player's body while it is in the tree. It is made anew in each zone, and
    // on a zone change the old one leaves the tree before it is freed.
    public Player? Player
    {
        get
        {
            Player? player = _node.GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Player;
            return player != null && player.IsInsideTree() ? player : null;
        }
    }

    // The zone this client has loaded; a client holds one at a time.
    public Zone? Zone
    {
        get { return FindFirst<Zone>(_node.GetTree().Root); }
    }

    // What the client knows of its player: zone, money, belongings. Null before the game
    // has started.
    public ClientView? View
    {
        get
        {
            ClientGame? client = _node.GetTree().Root.GetNodeOrNull<ClientGame>("Main/ClientGame");
            return client?.View;
        }
    }

    // The first visible screen of this type, or null.
    public T? Find<T>()
        where T : Control
    {
        return FindVisible<T>(_node.GetTree().Root);
    }

    // A real press and release at the control's centre, through the viewport, so the
    // click goes the way a player's does: a hidden, disabled or covered control does not
    // press.
    public void Click(Control control)
    {
        ClickAt(control, control.Size / 2);
    }

    // A click at a point inside a control, in its own coordinates: a spot on a picture.
    public void ClickAt(Control control, Vector2 local)
    {
        Vector2 point = control.GetGlobalTransformWithCanvas() * local;
        Viewport viewport = control.GetViewport();

        InputEventMouseButton press = new InputEventMouseButton();
        press.ButtonIndex = MouseButton.Left;
        press.Position = point;
        press.GlobalPosition = point;
        press.Pressed = true;
        viewport.PushInput(press, true);

        InputEventMouseButton release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        viewport.PushInput(release, true);
    }

    // The event a key bound to the action makes, down or up, fed through the input system.
    public void Key(string action, bool pressed)
    {
        InputEventAction key = new InputEventAction();
        key.Action = action;
        key.Pressed = pressed;
        Input.ParseInputEvent(key);
    }

    // A key held down or let go, for walking and turning.
    public void Hold(string action, bool down)
    {
        if (down && !_held.Contains(action))
        {
            Input.ActionPress(action);
            _held.Add(action);
        }
        else if (!down && _held.Contains(action))
        {
            Input.ActionRelease(action);
            _held.Remove(action);
        }
    }

    public void ReleaseAll()
    {
        foreach (string action in new List<string>(_held))
        {
            Hold(action, false);
        }
    }

    // Steps to run next, before the rest of the plan (a need met by another activity).
    public void InsertNext(List<BotStep> steps)
    {
        _insertNext(steps);
    }

    private static T? FindVisible<T>(Node node)
        where T : Control
    {
        T? found = node as T;

        if (found != null && found.IsVisibleInTree())
        {
            return found;
        }

        foreach (Node child in node.GetChildren())
        {
            T? inChild = FindVisible<T>(child);

            if (inChild != null)
            {
                return inChild;
            }
        }

        return null;
    }

    private static T? FindFirst<T>(Node node)
        where T : Node
    {
        T? found = node as T;

        if (found != null)
        {
            return found;
        }

        foreach (Node child in node.GetChildren())
        {
            T? inChild = FindFirst<T>(child);

            if (inChild != null)
            {
                return inChild;
            }
        }

        return null;
    }
}
