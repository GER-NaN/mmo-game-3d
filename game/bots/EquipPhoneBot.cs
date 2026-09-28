namespace MmoGame3d.Bots;

using System.IO;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Ui;

/// <summary>
/// A bot that enters the world as a new player, opens the inventory with the inventory
/// key, clicks Equip on the phone in the bag, and sees the phone in the Device slot. A
/// new player's starter kit has the phone in the bag (Belongings.StarterKit), so this
/// needs a fresh player (the Overseer's --fresh).
/// </summary>
public partial class EquipPhoneBot : Node
{
    private const double LookInterval = 0.25;
    private const double SettleSeconds = 1;

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private Stage _stage = Stage.WaitForWorld;
    private double _stageTime;
    private InventoryPanel? _inventory;

    private enum Stage
    {
        WaitForWorld,
        Settle,
        ReleaseKey,
        WaitForInventory,
        WaitForEquipped,
        Done,
    }

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("EquipPhoneBot: no --bot-folder given; the bot does nothing.");
            SetProcess(false);
            return;
        }

        _events = new BotEventLog(_folder);
        _events.Write("started", "");
    }

    public override void _Process(double delta)
    {
        _stageTime += delta;
        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return;
        }

        _sinceLook = 0;

        if (File.Exists(Path.Combine(_folder, Bot.StopFile)))
        {
            _events!.Write("stopped", "");
            _events.Close();
            SetProcess(false);
            GetTree().Quit();
            return;
        }

        switch (_stage)
        {
            case Stage.WaitForWorld:
                if (GetTree().GetFirstNodeInGroup(Player.LocalGroup) != null)
                {
                    _events!.Write("in-world", "");
                    Next(Stage.Settle);
                }

                break;
            case Stage.Settle:
                if (_stageTime >= SettleSeconds)
                {
                    PressAction("inventory", true);
                    _events!.Write("pressed", "inventory");
                    Next(Stage.ReleaseKey);
                }

                break;
            case Stage.ReleaseKey:
                PressAction("inventory", false);
                Next(Stage.WaitForInventory);
                break;
            case Stage.WaitForInventory:
                _inventory = Find<InventoryPanel>(GetTree().Root);

                if (_inventory != null)
                {
                    Button? equip = EquipButtonFor(_inventory, "Phone");

                    if (equip == null)
                    {
                        _events!.Write("failed", "no Equip button on a phone in the bag");
                        Next(Stage.Done);
                        break;
                    }

                    _events!.Write("inventory-open", "");
                    Click(equip);
                    _events.Write("clicked", "Equip on Phone");
                    Next(Stage.WaitForEquipped);
                }

                break;
            case Stage.WaitForEquipped:
                Label worn = _inventory!.GetNode<Label>("%Equipment/Device/ItemName");

                if (worn.Text.StartsWith("Phone"))
                {
                    _events!.Write("equipped", worn.Text);
                    _events.Write("done", "");
                    Next(Stage.Done);
                }

                break;
            case Stage.Done:
                break;
        }
    }

    private void Next(Stage stage)
    {
        _stage = stage;
        _stageTime = 0;
    }

    // The bag's rows are built from data and have no names yet (bots.md T2), so the row
    // is found by the start of its label.
    private static Button? EquipButtonFor(InventoryPanel inventory, string itemName)
    {
        foreach (Node row in inventory.GetNode("%Things").GetChildren())
        {
            HBoxContainer? box = row as HBoxContainer;

            if (box == null || box.IsQueuedForDeletion() || box.GetChildCount() < 2)
            {
                continue;
            }

            Label? label = box.GetChild(0) as Label;
            Button? button = box.GetChild(box.GetChildCount() - 1) as Button;

            if (label != null && button != null && label.Text.StartsWith(itemName) && button.Text == "Equip")
            {
                return button;
            }
        }

        return null;
    }

    // The same event a key bound to the action makes, fed through the input system.
    private static void PressAction(string action, bool pressed)
    {
        InputEventAction key = new InputEventAction();
        key.Action = action;
        key.Pressed = pressed;
        Input.ParseInputEvent(key);
    }

    // A real press and release at the control's centre, through the viewport, so the
    // click goes the way a player's does: a hidden, disabled or covered control does not
    // press.
    private static void Click(Control control)
    {
        Vector2 centre = control.GetGlobalTransformWithCanvas() * (control.Size / 2);
        Viewport viewport = control.GetViewport();

        InputEventMouseButton press = new InputEventMouseButton();
        press.ButtonIndex = MouseButton.Left;
        press.Position = centre;
        press.GlobalPosition = centre;
        press.Pressed = true;
        viewport.PushInput(press, true);

        InputEventMouseButton release = (InputEventMouseButton)press.Duplicate();
        release.Pressed = false;
        viewport.PushInput(release, true);
    }

    private static string FolderFromCommandLine()
    {
        string[] args = OS.GetCmdlineUserArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--bot-folder")
            {
                return args[i + 1];
            }
        }

        return "";
    }

    // The first visible control of this type anywhere in the tree.
    private static T? Find<T>(Node node)
        where T : Control
    {
        T? found = node as T;

        if (found != null && found.IsVisibleInTree())
        {
            return found;
        }

        foreach (Node child in node.GetChildren())
        {
            T? inChild = Find<T>(child);

            if (inChild != null)
            {
                return inChild;
            }
        }

        return null;
    }
}
