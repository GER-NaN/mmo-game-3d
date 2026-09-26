namespace MmoGame3d.Rules.Players;

using System.Globalization;

/// <summary>
/// How a player's character looks: a base look (see Looks), and colours for skin, hair,
/// top and bottom, with the backpack and glasses on or off. Kept and synced as one short
/// string, "a|2|5|3|1|1|0", in the place the look id used to be; an old bare look id
/// ("b") still reads, with everything else at its default. Index 0 of every colour list
/// is "Original": the texture's own colour. The colours are placeholders.
/// </summary>
public class Appearance
{
    public static readonly string[] SkinTones = { "", "#F6D2B8", "#E8B592", "#C68A63", "#9A6446", "#6B4430", "#4A2E22" };

    public static readonly string[] HairColors = { "", "#1E1A18", "#4A3122", "#8B5A2B", "#D8B26E", "#B24A2A", "#E8E4DC", "#3A7BD5", "#E062A6", "#3BB8A8" };

    public static readonly string[] ClothesColors = { "", "#E4E4E4", "#2E2E33", "#C0392B", "#E67E22", "#F1C40F", "#27AE60", "#2980B9", "#8E44AD", "#E062A6", "#7F8C8D", "#6D4C33" };

    public string Base { get; set; } = Looks.Default;
    public int Skin { get; set; }
    public int Hair { get; set; }
    public int Top { get; set; }
    public int Bottom { get; set; }
    public bool Backpack { get; set; } = true;
    public bool Glasses { get; set; } = true;

    // Anything unreadable or out of range comes back as the nearest valid appearance,
    // so a bad row or a modified client never breaks a character.
    public static Appearance Parse(string text)
    {
        Appearance appearance = new Appearance();
        string[] parts = (text ?? "").Split('|');
        appearance.Base = Looks.OrDefault(parts[0]);
        appearance.Skin = Index(parts, 1, SkinTones.Length);
        appearance.Hair = Index(parts, 2, HairColors.Length);
        appearance.Top = Index(parts, 3, ClothesColors.Length);
        appearance.Bottom = Index(parts, 4, ClothesColors.Length);
        appearance.Backpack = parts.Length <= 5 || parts[5] != "0";
        appearance.Glasses = parts.Length <= 6 || parts[6] != "0";
        return appearance;
    }

    public static string Normalize(string text)
    {
        return Parse(text).Format();
    }

    public string Format()
    {
        return string.Join("|", Base, Skin, Hair, Top, Bottom, Backpack ? 1 : 0, Glasses ? 1 : 0);
    }

    private static int Index(string[] parts, int at, int count)
    {
        int value;

        if (parts.Length <= at || !int.TryParse(parts[at], NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return 0;
        }

        return value >= 0 && value < count ? value : 0;
    }
}
