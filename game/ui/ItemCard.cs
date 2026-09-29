namespace MmoGame3d.Ui;

using Godot;

// The card shown when the mouse rests on an item: its name, a detail line (how many,
// or its charge), and what it is.
public partial class ItemCard : VBoxContainer
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://game/ui/ItemCard.tscn");

    public static ItemCard Make(string title, Color titleColor, string detail, string description)
    {
        ItemCard card = Scene.Instantiate<ItemCard>();
        Label titleLabel = card.GetNode<Label>("%Title");
        titleLabel.Text = title;
        titleLabel.AddThemeColorOverride("font_color", titleColor);
        Label detailLabel = card.GetNode<Label>("%Detail");
        detailLabel.Text = detail;
        detailLabel.Visible = detail != "";
        card.GetNode<Label>("%Description").Text = description;
        return card;
    }
}
