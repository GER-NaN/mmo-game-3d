namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Shops;

/// <summary>
/// A shop's offers, each with its price and a Buy button. Things the player cannot
/// afford still show, priced in red, and a Buy on them is refused by the server with
/// the reason: a player sees what exists before they can have it.
/// </summary>
public partial class ShopPanel : PanelContainer
{
    // Bots find the Buy buttons by this group, then click them like a person.
    public const string BuyGroup = "shop_buy";

    private static readonly Color TooDear = new Color(1f, 0.55f, 0.5f);

    private string _shopId = "";

    // The index of the offer, in the shop's list.
    public event Action<int>? BuyPressed;
    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

    public void ShowShop(string shopId, string keeperTitle, int dollars)
    {
        _shopId = shopId;
        GetNode<Label>("%Title").Text = keeperTitle;
        ShowDollars(dollars);
    }

    public void ShowDollars(int dollars)
    {
        GetNode<Label>("%Wallet").Text = "You have $" + dollars;
        VBoxContainer rows = GetNode<VBoxContainer>("%Offers");

        foreach (Node old in rows.GetChildren())
        {
            old.QueueFree();
        }

        IReadOnlyList<ShopOffer> offers = Shops.Offers(_shopId);

        for (int i = 0; i < offers.Count; i++)
        {
            ShopOffer offer = offers[i];
            HBoxContainer row = new HBoxContainer();

            Label name = new Label { Text = ItemCatalog.Describe(offer.Type, offer.Tier), SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = ItemCatalog.Get(offer.Type).Description, MouseFilter = MouseFilterEnum.Pass };
            Label price = new Label { Text = "$" + offer.Price };

            if (offer.Price > dollars)
            {
                price.AddThemeColorOverride("font_color", TooDear);
            }

            Button buy = new Button { Text = "Buy", FocusMode = FocusModeEnum.None };
            buy.AddToGroup(BuyGroup);
            int index = i;
            buy.Pressed += () => BuyPressed?.Invoke(index);

            row.AddChild(name);
            row.AddChild(price);
            row.AddChild(buy);
            rows.AddChild(row);
        }
    }
}
