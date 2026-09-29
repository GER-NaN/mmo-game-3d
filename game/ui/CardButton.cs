namespace MmoGame3d.Ui;

using Godot;

// A button whose tooltip is a card instead of plain text.
public partial class CardButton : Button
{
    public string CardTitle = "";
    public Color CardColor = Colors.White;
    public string CardDetail = "";
    public string CardDescription = "";

    public CardButton()
    {
        // Godot shows a tooltip only when the text is set.
        TooltipText = " ";
    }

    public override GodotObject _MakeCustomTooltip(string forText)
    {
        return TooltipCard.Make(CardTitle, CardColor, CardDetail, CardDescription);
    }
}
