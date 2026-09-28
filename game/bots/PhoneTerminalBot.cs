namespace MmoGame3d.Bots;

using System.IO;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;
using MmoGame3d.Ui;

/// <summary>
/// A bot that enters the world and opens the terminal from its phone, with the phone
/// key. The server lets a player online only with a phone worn in the Device slot, so if
/// the client's view says no phone is worn, it equips the one in the bag first (as
/// EquipPhoneBot does), closes the inventory, then presses the phone key.
/// </summary>
public partial class PhoneTerminalBot : Node
{
    private const double LookInterval = 0.25;
    private const double SettleSeconds = 1;

    private string _folder = "";
    private BotEventLog? _events;
    private double _sinceLook;
    private Stage _stage = Stage.WaitForWorld;
    private double _stageTime;
    private ClientGame? _client;

    // A key pressed on one look is let go on the next, then the bot goes on to this stage.
    private string _heldKey = "";
    private Stage _afterKey;

    private enum Stage
    {
        WaitForWorld,
        Settle,
        ReleaseKey,
        WaitForInventory,
        WaitForEquipped,
        WaitForTerminal,
        Done,
    }

    public override void _Ready()
    {
        _folder = FolderFromCommandLine();

        if (_folder.Length == 0)
        {
            GD.PushError("PhoneTerminalBot: no --bot-folder given; the bot does nothing.");
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
                _client = GetTree().Root.GetNodeOrNull<ClientGame>("Main/ClientGame");

                if (_client != null && GetTree().GetFirstNodeInGroup(Player.LocalGroup) != null)
                {
                    _events!.Write("in-world", _client.View.ZoneId);
                    Next(Stage.Settle);
                }

                break;
            case Stage.Settle:
                if (_stageTime >= SettleSeconds)
                {
                    if (PhoneWorn())
                    {
                        _events!.Write("phone", "already worn");
                        PressKey("phone", Stage.WaitForTerminal);
                    }
                    else
                    {
                        _events!.Write("phone", "not worn; equipping it first");
                        PressKey("inventory", Stage.WaitForInventory);
                    }
                }

                break;
            case Stage.ReleaseKey:
                PressAction(_heldKey, false);
                Next(_afterKey);
                break;
            case Stage.WaitForInventory:
                InventoryPanel? inventory = Find<InventoryPanel>(GetTree().Root);

                if (inventory != null)
                {
                    Button? equip = EquipButtonFor(inventory, "Phone");

                    if (equip == null)
                    {
                        _events!.Write("failed", "no Equip button on a phone in the bag");
                        Next(Stage.Done);
                        break;
                    }

                    Click(equip);
                    _events!.Write("clicked", "Equip on Phone");
                    Next(Stage.WaitForEquipped);
                }

                break;
            case Stage.WaitForEquipped:
                if (PhoneWorn())
                {
                    _events!.Write("phone", "worn");
                    PressKey("inventory", Stage.Settle);
                }

                break;
            case Stage.WaitForTerminal:
                if (Find<TerminalScreen>(GetTree().Root) != null)
                {
                    _events!.Write("terminal-open", "");
                    _events.Write("done", "");
                    Next(Stage.Done);
                }

                break;
            case Stage.Done:
                break;
        }
    }

    private bool PhoneWorn()
    {
        ItemInstance? device = _client!.View.Belongings.Equipped(SlotType.Device);
        return device != null && device.Type == ItemType.Phone;
    }

    private void PressKey(string action, Stage after)
    {
        PressAction(action, true);
        _events!.Write("pressed", action);
        _heldKey = action;
        _afterKey = after;
        Next(Stage.ReleaseKey);
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
