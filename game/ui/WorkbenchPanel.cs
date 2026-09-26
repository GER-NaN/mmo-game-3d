namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;

// At a workbench: each phone you have, its battery, and the swap. Take the old battery
// out, then put one in; the fullest you have goes in, and one still in its pack counts
// as full.
public partial class WorkbenchPanel : PanelContainer
{
    // Bots find the buttons by these groups, then click them like a person.
    public const string RemoveGroup = "workbench_remove";
    public const string InsertGroup = "workbench_insert";

    public event Action<Guid>? RemovePressed;
    public event Action<Guid>? InsertPressed;
    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

    public void ShowBench(IReadOnlyList<ItemStack> stacks, List<ItemInstance> instances)
    {
        Belongings mine = new Belongings(new Inventory(), instances);
        int packed = 0;
        int loose = 0;

        foreach (ItemStack stack in stacks)
        {
            if (stack.Type == ItemType.Battery)
            {
                packed += stack.Quantity;
            }
        }

        foreach (ItemInstance item in instances)
        {
            if (item.IsLoose && item.Type == ItemType.Battery)
            {
                loose++;
            }
        }

        GetNode<Label>("%Batteries").Text = "Batteries: " + packed + " new, " + loose + " loose";

        VBoxContainer phones = GetNode<VBoxContainer>("%Phones");

        foreach (Node old in phones.GetChildren())
        {
            old.QueueFree();
        }

        foreach (ItemInstance phone in instances)
        {
            if (phone.Type != ItemType.Phone)
            {
                continue;
            }

            ItemInstance? battery = mine.Inside(phone, SlotType.Battery);
            HBoxContainer row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = "Phone: " + (battery == null ? "no battery" : "battery " + Power.Percent(battery.Charge) + "%"),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });

            Guid id = phone.Id;
            Button button = new Button { Text = battery == null ? "Put in a battery" : "Take battery out", FocusMode = FocusModeEnum.None };
            button.AddToGroup(battery == null ? InsertGroup : RemoveGroup);

            if (battery == null)
            {
                button.Pressed += () => InsertPressed?.Invoke(id);
            }
            else
            {
                button.Pressed += () => RemovePressed?.Invoke(id);
            }

            row.AddChild(button);
            phones.AddChild(row);
        }

        if (phones.GetChildCount() == 0)
        {
            phones.AddChild(new Label { Text = "You have nothing to work on here." });
        }
    }
}
