namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;

// Giving to the selected player: one or all of a stack, or some pocket change. The
// server checks you are close enough and have it; this only asks.
public partial class GivePanel : PanelContainer
{
    // (type, tier, quantity).
    public event Action<ItemType, ItemTier, int>? GivePressed;
    public event Action<int>? GiveDollarsPressed;
    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
        GetNode<Button>("%GiveMoney").Pressed += () => GiveDollarsPressed?.Invoke((int)GetNode<SpinBox>("%Amount").Value);
    }

    public void ShowFor(string targetName, IReadOnlyList<ItemStack> stacks, int dollars)
    {
        GetNode<Label>("%Title").Text = "Give to " + targetName;
        GetNode<Label>("%MoneyLabel").Text = "You have $" + dollars;

        VBoxContainer rows = GetNode<VBoxContainer>("%Stacks");

        foreach (Node old in rows.GetChildren())
        {
            old.QueueFree();
        }

        foreach (ItemStack stack in stacks)
        {
            HBoxContainer row = new HBoxContainer();
            row.AddChild(new Label { Text = stack.Quantity + " x " + ItemCatalog.Describe(stack.Type, stack.Tier), SizeFlagsHorizontal = SizeFlags.ExpandFill });

            ItemType type = stack.Type;
            ItemTier tier = stack.Tier;
            int all = stack.Quantity;

            Button one = new Button { Text = "Give 1", FocusMode = FocusModeEnum.None };
            one.Pressed += () => GivePressed?.Invoke(type, tier, 1);
            row.AddChild(one);

            if (all > 1)
            {
                Button every = new Button { Text = "Give all", FocusMode = FocusModeEnum.None };
                every.Pressed += () => GivePressed?.Invoke(type, tier, all);
                row.AddChild(every);
            }

            rows.AddChild(row);
        }

        if (stacks.Count == 0)
        {
            rows.AddChild(new Label { Text = "You carry nothing you can give." });
        }
    }
}
