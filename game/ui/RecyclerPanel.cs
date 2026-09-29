namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;

/// <summary>
/// The recycler: what the player could put in, and what each pays. Stacks go in one at
/// a time; a thing goes in with its parts. Worn things are not listed: unequip first.
/// Buttons never take focus, so walking goes on after a click.
/// </summary>
public partial class RecyclerPanel : PanelContainer
{
    // (type, tier) of a stack, one of it.
    public event Action<ItemType, ItemTier>? RecycleOnePressed;

    // (instance id) of a loose thing.
    public event Action<Guid>? RecycleThingPressed;
    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

    public void ShowBag(IReadOnlyList<ItemStack> stacks, List<ItemInstance> instances, int dollars)
    {
        GetNode<Label>("%Wallet").Text = "You have $" + dollars;
        VBoxContainer rows = GetNode<VBoxContainer>("%Rows");

        foreach (Node old in rows.GetChildren())
        {
            rows.RemoveChild(old);
            old.QueueFree();
        }

        foreach (ItemStack stack in stacks)
        {
            ItemType type = stack.Type;
            ItemTier tier = stack.Tier;
            AddRow(rows, "Stack_" + type + "_" + tier, ItemCatalog.Describe(type, tier) + "  x" + stack.Quantity, Recycling.ValueOf(type, tier), "Recycle one", () => RecycleOnePressed?.Invoke(type, tier));
        }

        Belongings mine = new Belongings(new Inventory(), instances);

        foreach (ItemInstance thing in instances)
        {
            if (!thing.IsLoose)
            {
                continue;
            }

            int value = Recycling.ValueOf(thing.Type, thing.Tier);
            string text = ItemCatalog.Describe(thing.Type, thing.Tier);

            foreach (ItemInstance inside in instances)
            {
                if (inside.ParentId == thing.Id)
                {
                    value += Recycling.ValueOf(inside.Type, inside.Tier);
                    text += " + " + ItemCatalog.Describe(inside.Type, inside.Tier);
                }
            }

            if (thing.Charge != null)
            {
                text += "  " + Power.Percent(thing.Charge) + "%";
            }

            Guid id = thing.Id;
            AddRow(rows, thing.Type + "_" + id, text, value, "Recycle", () => RecycleThingPressed?.Invoke(id));
        }

        if (rows.GetChildCount() == 0)
        {
            rows.AddChild(new Label { Text = "Nothing to recycle. Worn things must be unequipped first.", Modulate = new Color(1f, 1f, 1f, 0.6f), AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }
    }

    // name: what the row holds, so it can be found by it (bots.md T2): "Stack_Battery_Standard"
    // for a stack, "Phone_<id>" for one thing.
    private static void AddRow(VBoxContainer rows, string name, string text, int value, string action, Action pressed)
    {
        HBoxContainer row = new HBoxContainer { Name = name };
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(new Label { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        row.AddChild(new Label { Text = "$" + value });
        Button button = new Button { Name = "Recycle", Text = action, FocusMode = FocusModeEnum.None };
        button.Pressed += pressed;
        row.AddChild(button);
        rows.AddChild(row);
    }
}
