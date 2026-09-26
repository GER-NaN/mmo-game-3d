namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;

// What you carry, as the server last said: pocket change, stacks, and the things with an
// identity (a phone with its battery), each with what can be done to it here. It never
// takes keyboard focus, so walking goes on while it is open.
public partial class InventoryPanel : PanelContainer
{
    // Bots find the Equip and Drop buttons by these groups, then click them like a person.
    public const string EquipGroup = "inventory_equip";
    public const string DropGroup = "inventory_drop";

    private static readonly Color[] TierColors =
    {
        new Color(0.85f, 0.85f, 0.85f),
        new Color(0.45f, 0.9f, 0.5f),
        new Color(0.5f, 0.65f, 1f),
        new Color(0.85f, 0.5f, 1f),
    };

    public event Action<Guid>? EquipPressed;
    public event Action? RepairPackPressed;

    // Bots find the repair pack button by this group.
    public const string RepairPackGroup = "inventory_repair_pack";

    // A Mechanical Engineer carries a repair pack: workbench work anywhere.
    public void ShowRepairPack(bool engineer)
    {
        Button? pack = GetNodeOrNull<Button>("Margin/Rows/RepairPack");

        if (engineer && pack == null)
        {
            pack = new Button { Name = "RepairPack", Text = "Open repair pack", FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
            pack.AddToGroup(RepairPackGroup);
            pack.Pressed += () => RepairPackPressed?.Invoke();
            GetNode<VBoxContainer>("Margin/Rows").AddChild(pack);
            GetNode<VBoxContainer>("Margin/Rows").MoveChild(pack, 2);
        }
        else if (!engineer && pack != null)
        {
            pack.QueueFree();
        }
    }
    public event Action<Guid>? UnequipPressed;

    // (type, tier, quantity): the whole stack.
    public event Action<ItemType, ItemTier, int>? DropPressed;

    public void ShowBag(IReadOnlyList<ItemStack> stacks, int dollars, List<ItemInstance> instances)
    {
        GetNode<Label>("%Dollars").Text = "Pocket change: $" + dollars;

        VBoxContainer list = GetNode<VBoxContainer>("%Items");

        foreach (Node old in list.GetChildren())
        {
            old.QueueFree();
        }

        foreach (ItemStack stack in stacks)
        {
            HBoxContainer row = new HBoxContainer();
            Label name = new Label
            {
                Text = stack.Quantity + " x " + ItemCatalog.Describe(stack.Type, stack.Tier),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = ItemCatalog.Get(stack.Type).Description,
                MouseFilter = MouseFilterEnum.Pass,
            };
            name.AddThemeColorOverride("font_color", TierColors[(int)stack.Tier]);
            row.AddChild(name);

            ItemType type = stack.Type;
            ItemTier tier = stack.Tier;
            int quantity = stack.Quantity;
            Button drop = new Button { Text = "Drop", FocusMode = FocusModeEnum.None };
            drop.AddToGroup(DropGroup);
            drop.Pressed += () => DropPressed?.Invoke(type, tier, quantity);
            row.AddChild(drop);
            list.AddChild(row);
        }

        list.Visible = stacks.Count > 0;
        ShowThings(new Belongings(new Inventory(), instances));
        GetNode<Label>("%Empty").Visible = stacks.Count == 0 && instances.Count == 0;
    }

    // Only the things at the top: a battery inside a phone shows as the phone's charge.
    // "Phone  battery 43%", "Battery  60%", "EMP Emitter".
    private static string Describe(Belongings mine, ItemInstance item)
    {
        string text = ItemCatalog.Describe(item.Type, item.Tier);

        if (item.Type == ItemType.Phone)
        {
            ItemInstance? battery = mine.Inside(item, SlotType.Battery);
            text += battery == null ? "  (no battery)" : "  battery " + Power.Percent(battery.Charge) + "%";
        }
        else if (item.Charge != null)
        {
            text += "  " + Power.Percent(item.Charge) + "%";
        }

        return text;
    }

    private void ShowThings(Belongings mine)
    {
        VBoxContainer things = GetNode<VBoxContainer>("%Things");

        foreach (Node old in things.GetChildren())
        {
            old.QueueFree();
        }

        Label equippedTitle = new Label { Text = "Equipped" };
        equippedTitle.AddThemeFontSizeOverride("font_size", 16);
        things.AddChild(equippedTitle);

        foreach (SlotType slot in Belongings.PlayerSlots)
        {
            ItemInstance? worn = mine.Equipped(slot);
            HBoxContainer slotRow = new HBoxContainer();
            string slotText = slot + ":  " + (worn == null ? "empty" : Describe(mine, worn));
            slotRow.AddChild(new Label { Text = slotText, SizeFlagsHorizontal = SizeFlags.ExpandFill, Modulate = worn == null ? new Color(1f, 1f, 1f, 0.5f) : Colors.White });

            if (worn != null)
            {
                Guid wornId = worn.Id;
                Button unequip = new Button { Text = "Unequip", FocusMode = FocusModeEnum.None };
                unequip.Pressed += () => UnequipPressed?.Invoke(wornId);
                slotRow.AddChild(unequip);
            }

            things.AddChild(slotRow);
        }

        Label bagTitle = new Label { Text = "In the bag" };
        bagTitle.AddThemeFontSizeOverride("font_size", 16);
        things.AddChild(bagTitle);

        foreach (ItemInstance item in mine.Instances)
        {
            // Inside something, or worn: shown above.
            if (item.ParentId != null || item.Slot != null)
            {
                continue;
            }

            HBoxContainer row = new HBoxContainer();
            string text = Describe(mine, item);

            row.AddChild(new Label { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = ItemCatalog.Get(item.Type).Description, MouseFilter = MouseFilterEnum.Pass });

            if (Belongings.SlotFor(item.Type) != null)
            {
                Guid id = item.Id;
                Button button = new Button { Text = item.Slot == null ? "Equip" : "Unequip", FocusMode = FocusModeEnum.None };

                if (item.Slot == null)
                {
                    button.AddToGroup(EquipGroup);
                    button.Pressed += () => EquipPressed?.Invoke(id);
                }
                else
                {
                    button.Pressed += () => UnequipPressed?.Invoke(id);
                }

                row.AddChild(button);
            }

            things.AddChild(row);
        }
    }
}
