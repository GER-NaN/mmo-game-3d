namespace MmoGame3d.Ui;

using Godot;

// A label whose tooltip is an item card instead of plain text.
public partial class ItemLabel : Label
{
    public string CardTitle = "";
    public Color CardColor = Colors.White;
    public string CardDetail = "";
    public string CardDescription = "";

    public ItemLabel()
    {
        // Godot shows a tooltip only when the text is set.
        TooltipText = " ";
    }

    public override GodotObject _MakeCustomTooltip(string forText)
    {
        return ItemCard.Make(CardTitle, CardColor, CardDetail, CardDescription);
    }
}
