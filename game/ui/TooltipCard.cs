namespace MmoGame3d.Ui;

using Godot;

// The card shown when the mouse rests on a thing (an item, a skill): its name, a detail
// line (how many, a charge, a level), and what it is.
public partial class TooltipCard : VBoxContainer
{
    private static readonly PackedScene Scene = GD.Load<PackedScene>("res://game/ui/TooltipCard.tscn");

    public static TooltipCard Make(string title, Color titleColor, string detail, string description)
    {
        TooltipCard card = Scene.Instantiate<TooltipCard>();
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
