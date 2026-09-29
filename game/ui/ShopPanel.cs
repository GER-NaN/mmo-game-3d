namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Shops;

/// <summary>
/// A shop's offers, each with what it is, its price and a Buy button. Things the
/// player cannot afford still show, priced in red, and a Buy on them is refused by the
/// server with the reason: a player sees what exists before they can have it.
/// </summary>
public partial class ShopPanel : PanelContainer
{
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

        // Out of the list at once, so the new rows can take the old rows' names.
        foreach (Node old in rows.GetChildren())
        {
            rows.RemoveChild(old);
            old.QueueFree();
        }

        IReadOnlyList<ShopOffer> offers = Shops.Offers(_shopId);

        for (int i = 0; i < offers.Count; i++)
        {
            ShopOffer offer = offers[i];
            // Named for its offer, so the row can be found by what it sells (bots.md T2).
            HBoxContainer row = new HBoxContainer { Name = offer.Type + "_" + offer.Tier };
            row.AddThemeConstantOverride("separation", 16);

            VBoxContainer what = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            what.AddThemeConstantOverride("separation", 2);
            Label name = new Label { Text = ItemCatalog.Describe(offer.Type, offer.Tier) };
            name.AddThemeFontSizeOverride("font_size", 18);
            what.AddChild(name);
            what.AddChild(new Label { Text = ItemCatalog.Get(offer.Type).Description, AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1f, 1f, 1f, 0.7f) });

            Label price = new Label { Text = "$" + offer.Price, CustomMinimumSize = new Vector2(64, 0), HorizontalAlignment = HorizontalAlignment.Right };
            price.AddThemeFontSizeOverride("font_size", 18);

            if (offer.Price > dollars)
            {
                price.AddThemeColorOverride("font_color", TooDear);
            }

            Button buy = new Button { Name = "Buy", Text = "Buy", FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(90, 36), SizeFlagsVertical = SizeFlags.ShrinkCenter };
            int index = i;
            buy.Pressed += () => BuyPressed?.Invoke(index);

            row.AddChild(what);
            row.AddChild(price);
            row.AddChild(buy);
            rows.AddChild(row);
        }
    }
}
