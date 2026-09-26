namespace MmoGame3d.Ui;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;

// What you carry, as the server last said. It never takes keyboard focus, so walking
// goes on while it is open.
public partial class InventoryPanel : PanelContainer
{
    private static readonly Color[] TierColors =
    {
        new Color(0.85f, 0.85f, 0.85f),
        new Color(0.45f, 0.9f, 0.5f),
        new Color(0.5f, 0.65f, 1f),
        new Color(0.85f, 0.5f, 1f),
    };

    public void ShowStacks(IReadOnlyList<ItemStack> stacks, int dollars)
    {
        GetNode<Label>("%Dollars").Text = "Pocket change: $" + dollars;
        ItemList list = GetNode<ItemList>("%Items");
        list.Clear();

        foreach (ItemStack stack in stacks)
        {
            int row = list.AddItem(stack.Quantity + " x " + ItemCatalog.Describe(stack.Type, stack.Tier));
            list.SetItemCustomFgColor(row, TierColors[(int)stack.Tier]);
            list.SetItemTooltip(row, ItemCatalog.Get(stack.Type).Description);
        }

        list.Visible = stacks.Count > 0;
        GetNode<Label>("%Empty").Visible = stacks.Count == 0;
    }
}
