namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Players;
using MmoGame3d.Rules.Movement;
using MmoGame3d.Ui;
using MmoGame3d.Zones;

/// <summary>
/// What a bot can see and do. It looks at the screen and the scene as a person looks at
/// them, and it acts only through input (keys, clicks, typing: the events a keyboard and
/// a mouse make), so everything past the input is the real game. What is open is always
/// looked up, never remembered, so a bot that lost track of itself still sees the truth.
/// </summary>
public sealed class BotBody
{
    // Held this long, like a key press, so the game sees the key down and then up.
    private const double TapSeconds = 0.1;

    // Roughly facing: walk while turning. Further round: turn on the spot first.
    private const float FacingSlack = 0.12f;
    private const float WalkWhileTurning = 0.8f;

    private readonly Node _node;
    private double _interactHeld = -1;

    public BotBody(Node node, Random random)
    {
        _node = node;
        Random = random;
    }

    public Random Random { get; }

    private SceneTree Tree
    {
        get { return _node.GetTree(); }
    }

    // ---------------------------------------------------------------- senses

    public Player? Me
    {
        get { return Tree.GetFirstNodeInGroup(Player.LocalGroup) as Player; }
    }

    // A player's body stands in its zone's Players.
    public Zone? Zone
    {
        get
        {
            Player? me = Me;
            return me == null ? null : me.GetParent()?.GetParent() as Zone;
        }
    }

    public string ZoneId
    {
        get
        {
            Zone? zone = Zone;
            return zone == null ? "" : zone.ZoneId;
        }
    }

    // The "[F] ..." line: what F would use now, or "" when nothing is in reach.
    public string Prompt
    {
        get
        {
            Label? label = Tree.GetFirstNodeInGroup(Hud.PromptGroup) as Label;
            return label != null && label.Visible ? label.Text : "";
        }
    }

    public bool IsOnline
    {
        get { return Tree.GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) != null; }
    }

    public Button? Usable(string group)
    {
        foreach (Node node in Tree.GetNodesInGroup(group))
        {
            Button? button = node as Button;

            if (button != null && !button.Disabled && button.IsVisibleInTree())
            {
                return button;
            }
        }

        return null;
    }

    public List<Button> UsableAll(string group)
    {
        List<Button> buttons = new List<Button>();

        foreach (Node node in Tree.GetNodesInGroup(group))
        {
            Button? button = node as Button;

            if (button != null && !button.Disabled && button.IsVisibleInTree())
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    // Something standing in the current zone, by its path under the zone.
    public Node3D? Thing(string path)
    {
        // A zone on its way out is out of the tree before it is freed.
        Zone? zone = Zone;
        Node3D? thing = zone == null || !zone.IsInsideTree() ? null : zone.GetNodeOrNull<Node3D>(path);
        return thing != null && thing.IsInsideTree() ? thing : null;
    }

    public float DistanceTo(Vector3 point)
    {
        Player? me = Me;

        if (me == null)
        {
            return float.MaxValue;
        }

        Vector3 apart = point - me.GlobalPosition;
        apart.Y = 0f;
        return apart.Length();
    }

    // Everything open over the world: panels, screens, the game menu.
    public List<Control> OpenPanels()
    {
        List<Control> open = new List<Control>();
        Node? ui = _node.GetParent()?.GetNodeOrNull("Ui");

        if (ui == null)
        {
            return open;
        }

        foreach (Node node in ui.FindChildren("*", "Control", true, false))
        {
            Control? control = node as Control;

            if (control != null && IsPanel(control) && control.IsVisibleInTree())
            {
                open.Add(control);
            }
        }

        return open;
    }

    public bool IsOpen<T>()
        where T : Control
    {
        foreach (Control panel in OpenPanels())
        {
            if (panel is T)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPanel(Control control)
    {
        return control is InventoryPanel || control is SkillsPanel || control is SocialPanel || control is MapPanel
            || control is InGameMenu || control is SettingsPanel || control is ShopPanel || control is WorkbenchPanel
            || control is GivePanel || control is RecyclerPanel || control is CollegePanel || control is VisitorBookPanel
            || control is GardenScreen || control is PlantCard;
    }

    // ---------------------------------------------------------------- hands

    // Called every frame, so a tap of F lets go after its moment.
    public void Tick(double delta)
    {
        if (_interactHeld >= 0)
        {
            _interactHeld += delta;

            if (_interactHeld >= TapSeconds)
            {
                _interactHeld = -1;
                Input.ActionRelease("interact");
            }
        }
    }

    public void Interact()
    {
        Stop();
        Input.ActionPress("interact");
        _interactHeld = 0;
    }

    public void Click(Button button)
    {
        BotDriver.Click(button.GetGlobalRect().GetCenter());
    }

    // A key as an input event, for what the game takes from events (menus, the bag,
    // the phone) rather than from polled actions.
    public static void Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
    }

    // Opens the chat and types a line: talk, or an emote ("/wave").
    public void Chat(string text)
    {
        Stop();
        Press("chat");
        BotDriver.Type(text);
    }

    public void Stop()
    {
        Input.ActionRelease("move_forward");
        Input.ActionRelease("move_back");
        Input.ActionRelease("turn_left");
        Input.ActionRelease("turn_right");
        Input.ActionRelease("jump");
    }

    // Turns toward a point and walks once roughly facing it. The distance left.
    public float SteerTo(Vector3 target)
    {
        Player? me = Me;

        if (me == null)
        {
            return float.MaxValue;
        }

        // A focused text field takes the keys; walking needs them.
        _node.GetViewport().GuiReleaseFocus();
        Vector3 apart = target - me.GlobalPosition;
        apart.Y = 0f;

        // Heading 0 faces -Z, and a positive heading is turned left (Walking).
        float wanted = Mathf.Atan2(-apart.X, -apart.Z);
        float off = Walking.WrapAngle(wanted - me.Heading);
        Input.ActionRelease("turn_left");
        Input.ActionRelease("turn_right");

        if (off > FacingSlack)
        {
            Input.ActionPress("turn_left");
        }
        else if (off < -FacingSlack)
        {
            Input.ActionPress("turn_right");
        }

        if (Mathf.Abs(off) < WalkWhileTurning)
        {
            Input.ActionPress("move_forward");
        }
        else
        {
            Input.ActionRelease("move_forward");
        }

        return apart.Length();
    }

    // One step toward nothing open: goes offline, closes the game menu or a settings
    // screen with their buttons, a panel with its own key, anything else with Esc. True
    // when nothing is open. Esc on the world opens the game menu, so it is never pressed
    // unless something Esc closes is open.
    public bool CloseOne()
    {
        if (IsOnline)
        {
            Button? goOffline = Tree.GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup) as Button;

            if (goOffline != null && _node.GetViewport().GetVisibleRect().Encloses(goOffline.GetGlobalRect()))
            {
                Click(goOffline);
            }
            else
            {
                Press("ui_cancel");
            }

            return false;
        }

        foreach (Control panel in OpenPanels())
        {
            if (panel is InventoryPanel)
            {
                Press("inventory");
            }
            else if (panel is SkillsPanel)
            {
                Press("skills");
            }
            else if (panel is SocialPanel)
            {
                Press("social");
            }
            else if (panel is MapPanel)
            {
                Press("map");
            }
            else if (panel is SettingsPanel)
            {
                Button? back = Usable(SettingsPanel.BackGroup);

                if (back != null)
                {
                    Click(back);
                }
            }
            else if (panel is InGameMenu)
            {
                Button? resume = Usable(InGameMenu.ResumeGroup);

                if (resume != null)
                {
                    Click(resume);
                }
            }
            else
            {
                Press("ui_cancel");
            }

            return false;
        }

        return true;
    }
}
