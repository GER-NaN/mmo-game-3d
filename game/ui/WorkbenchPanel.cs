namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;

// At a workbench: each phone you have, its battery, and the swap. Take the old battery
// out, then choose which goes in: a new one from its pack, or one of the loose ones.
public partial class WorkbenchPanel : PanelContainer
{
    public event Action<Guid>? RemovePressed;
    // The phone, and the battery: Belongings.NewBattery or a loose battery's id.
    public event Action<Guid, string>? InsertPressed;
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

            if (battery != null)
            {
                Button remove = new Button { Text = "Take battery out", FocusMode = FocusModeEnum.None };
                remove.Pressed += () => RemovePressed?.Invoke(id);
                row.AddChild(remove);
                phones.AddChild(row);
                continue;
            }

            phones.AddChild(row);

            // No battery in: a button for each one that could go in, fullest first.
            if (packed > 0)
            {
                phones.AddChild(InsertButton(id, Belongings.NewBattery, "Put in a new battery (100%)"));
            }

            List<ItemInstance> choices = new List<ItemInstance>();

            foreach (ItemInstance item in instances)
            {
                if (item.IsLoose && item.Type == ItemType.Battery)
                {
                    choices.Add(item);
                }
            }

            choices.Sort((a, b) => (b.Charge ?? 0f).CompareTo(a.Charge ?? 0f));

            foreach (ItemInstance choice in choices)
            {
                phones.AddChild(InsertButton(id, choice.Id.ToString(), "Put in the loose battery at " + Power.Percent(choice.Charge) + "%"));
            }

            if (packed == 0 && choices.Count == 0)
            {
                phones.AddChild(new Label { Text = "    No battery to put in. The electronics shop sells them." });
            }
        }

        // Checked before the frees above land: nothing but freed rows means no phones.
        if (!HasPhone(instances))
        {
            phones.AddChild(new Label { Text = "You have nothing to work on here." });
        }
    }

    private Button InsertButton(Guid phone, string battery, string text)
    {
        Button button = new Button { Text = text, FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkEnd };
        button.Pressed += () => InsertPressed?.Invoke(phone, battery);
        return button;
    }

    private static bool HasPhone(List<ItemInstance> instances)
    {
        foreach (ItemInstance item in instances)
        {
            if (item.Type == ItemType.Phone)
            {
                return true;
            }
        }

        return false;
    }
}
