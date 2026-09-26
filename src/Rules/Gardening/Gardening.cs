namespace MmoGame3d.Rules.Gardening;

using System.Globalization;
using System.Text;

/// <summary>
/// The greenhouse's parts (Tiny Treats House Plants, CC0): pots, the pieces a plant is
/// built from (leaves, branches, vines, cacti, succulents), and the small ready-made
/// plants given as a reward. An id is the model's file name, the same on both sides.
/// </summary>
public static class PlantParts
{
    // Pots: (id, radius, height). The soil sits a little below the rim; see SoilHeight.
    private static readonly Dictionary<string, float[]> PotSizes = new Dictionary<string, float[]>
    {
        { "pot_A_small", new[] { 0.25f, 0.4f } },
        { "pot_A_medium", new[] { 0.5f, 0.7f } },
        { "pot_A_large", new[] { 0.75f, 1f } },
        { "pot_B_small", new[] { 0.25f, 0.4f } },
        { "pot_B_medium", new[] { 0.5f, 0.75f } },
        { "pot_B_large", new[] { 0.75f, 1f } },
        { "pot_C_small", new[] { 0.25f, 0.4f } },
        { "pot_C_medium", new[] { 0.5f, 0.75f } },
        { "pot_C_large", new[] { 0.75f, 1f } },
        { "pot_D_small", new[] { 0.25f, 0.4f } },
        { "pot_D_medium", new[] { 0.5f, 0.75f } },
        { "Pot_D_large", new[] { 0.75f, 1f } },
    };

    public static readonly string[] Pots =
    {
        "pot_A_small", "pot_A_medium", "pot_A_large", "pot_B_small", "pot_B_medium", "pot_B_large",
        "pot_C_small", "pot_C_medium", "pot_C_large", "pot_D_small", "pot_D_medium", "Pot_D_large",
    };

    // The families of pieces, each with its sizes and variants, in the order the tray
    // shows them.
    public static readonly string[][] PieceFamilies =
    {
        new[] { "Monstera", "monstera_leaf_large_A", "monstera_leaf_large_B", "monstera_leaf_large_C", "monstera_leaf_large_D", "monstera_leaf_medium_A", "monstera_leaf_medium_B", "monstera_leaf_medium_C", "monstera_leaf_small_A", "monstera_leaf_small_B", "monstera_leaf_small_C" },
        new[] { "Snake plant", "sansevieria_leaf_large_A", "sansevieria_leaf_large_B", "sansevieria_leaf_large_C", "sansevieria_leaf_medium_A", "sansevieria_leaf_medium_B", "sansevieria_leaf_medium_C", "sansevieria_leaf_small_A", "sansevieria_leaf_small_B", "sansevieria_leaf_small_C" },
        new[] { "Pothos", "pothos_vine_large_A", "pothos_vine_large_B", "pothos_vine_large_C", "pothos_vine_large_D", "pothos_vine_medium_A", "pothos_vine_medium_B", "pothos_vine_medium_C", "pothos_vine_medium_D", "pothos_vine_small_A", "pothos_vine_small_B", "pothos_vine_small_C", "pothos_vine_small_D" },
        new[] { "Yucca", "yucca_branch_large_A", "yucca_branch_large_B", "yucca_branch_large_C", "yucca_branch_medium_A", "yucca_branch_medium_B", "yucca_branch_medium_C", "yucca_branch_small_A", "yucca_branch_small_B", "yucca_branch_small_C" },
        new[] { "ZZ plant", "zzplant_leaf_large_A", "zzplant_leaf_large_B", "zzplant_leaf_large_C", "zzplant_leaf_medium_A", "zzplant_leaf_medium_B", "zzplant_leaf_medium_C", "zzplant_leaf_small_A", "zzplant_leaf_small_B", "zzplant_leaf_small_C" },
        new[] { "Cactus", "cactus_A", "cactus_B", "cactus_C", "cactus_D" },
        new[] { "Succulent", "succulent_A", "succulent_B", "succulent_C", "succulent_D" },
    };

    private static readonly HashSet<string> Pieces = AllPieces();

    public static bool IsPot(string id)
    {
        return PotSizes.ContainsKey(id);
    }

    public static bool IsPiece(string id)
    {
        return Pieces.Contains(id);
    }

    public static float PotRadius(string pot)
    {
        return PotSizes[pot][0];
    }

    public static float PotHeight(string pot)
    {
        return PotSizes[pot][1];
    }

    // The soil: a disc a little inside the rim and a little below it. Placeholders,
    // judged from the models.
    public static float SoilRadius(string pot)
    {
        return PotSizes[pot][0] * 0.75f;
    }

    public static float SoilHeight(string pot)
    {
        return PotSizes[pot][1] * 0.88f;
    }

    // The pot styles by the letter in their ids, named for how they look.
    private static readonly Dictionary<string, string> PotStyles = new Dictionary<string, string>
    {
        { "A", "Terracotta" },
        { "B", "Slate glaze" },
        { "C", "Rustic clay" },
        { "D", "Chalk white" },
    };

    // "pot_A_medium" reads "Terracotta pot, medium".
    public static string DescribePot(string pot)
    {
        string[] words = pot.Split('_');
        string? style;

        if (words.Length < 3 || !PotStyles.TryGetValue(words[1], out style))
        {
            return pot;
        }

        return style + " pot, " + words[2];
    }

    // "monstera_leaf_large_A" reads "Monstera leaf large A".
    public static string Describe(string id)
    {
        if (IsPot(id))
        {
            return DescribePot(id);
        }

        string[] words = id.Replace("sansevieria", "snake plant").Replace("zzplant", "ZZ plant").Split('_');
        StringBuilder text = new StringBuilder();

        for (int i = 0; i < words.Length; i++)
        {
            text.Append(i == 0 ? char.ToUpperInvariant(words[i][0]) + words[i].Substring(1) : " " + words[i]);
        }

        return text.ToString();
    }

    private static HashSet<string> AllPieces()
    {
        HashSet<string> all = new HashSet<string>();

        foreach (string[] family in PieceFamilies)
        {
            for (int i = 1; i < family.Length; i++)
            {
                all.Add(family[i]);
            }
        }

        return all;
    }
}

/// <summary>
/// One piece of a plant: where it stands on the soil, as a fraction of the soil's
/// radius (so a design survives a change of pot size), how it is turned (yaw), leaned
/// out (tilt) and sized.
/// </summary>
public class PlantPiece
{
    public const float MaxTiltRadians = 0.8f;
    public const float MinScale = 0.6f;
    public const float MaxScale = 1.4f;

    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Yaw { get; set; }
    public float Tilt { get; set; }
    public float Scale { get; set; } = 1f;
}

/// <summary>
/// A player's house plant as a design: a pot and up to five pieces. The client builds
/// it; the server checks it before it becomes a plant in the world, and the design is
/// all that is stored: anyone can rebuild the plant from it. Text form:
/// "pot;piece,x,z,yaw,tilt,scale;piece,...".
/// </summary>
public class PlantDesign
{
    public const int MaxPieces = 5;
    public const int MaxNameLength = 32;

    public string Pot { get; set; } = "pot_A_medium";
    public List<PlantPiece> Pieces { get; } = new List<PlantPiece>();

    public string Format()
    {
        StringBuilder text = new StringBuilder(Pot);

        foreach (PlantPiece piece in Pieces)
        {
            text.Append(';').Append(piece.Id);

            foreach (float value in new[] { piece.X, piece.Z, piece.Yaw, piece.Tilt, piece.Scale })
            {
                text.Append(',').Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        return text.ToString();
    }

    // Null when the text is not a valid design: an unknown pot or piece, none or too many
    // pieces, a piece off the soil, a lean or a size out of range.
    public static PlantDesign? Parse(string text)
    {
        string[] parts = (text ?? "").Split(';');

        if (parts.Length < 2 || parts.Length > MaxPieces + 1 || !PlantParts.IsPot(parts[0]))
        {
            return null;
        }

        PlantDesign design = new PlantDesign { Pot = parts[0] };

        for (int i = 1; i < parts.Length; i++)
        {
            string[] fields = parts[i].Split(',');
            float[] values = new float[5];

            if (fields.Length != 6 || !PlantParts.IsPiece(fields[0]))
            {
                return null;
            }

            for (int v = 0; v < 5; v++)
            {
                if (!float.TryParse(fields[v + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out values[v]) || !float.IsFinite(values[v]))
                {
                    return null;
                }
            }

            PlantPiece piece = new PlantPiece { Id = fields[0], X = values[0], Z = values[1], Yaw = values[2], Tilt = values[3], Scale = values[4] };

            if ((piece.X * piece.X) + (piece.Z * piece.Z) > 1.0001f
                || piece.Tilt < 0f || piece.Tilt > PlantPiece.MaxTiltRadians
                || piece.Scale < PlantPiece.MinScale || piece.Scale > PlantPiece.MaxScale)
            {
                return null;
            }

            design.Pieces.Add(piece);
        }

        return design;
    }
}

/// <summary>
/// What finishing a plant gives: a small ready-made potted plant, picked at random, and
/// Gardening experience. Placeholders.
/// </summary>
public static class GardenRewards
{
    public const long GardeningXp = 30;

    public static readonly Items.ItemType[] SmallPlants =
    {
        Items.ItemType.PottedMonstera,
        Items.ItemType.PottedPothos,
        Items.ItemType.PottedSnakePlant,
        Items.ItemType.PottedYucca,
        Items.ItemType.PottedZzPlant,
    };

    // The model a small potted plant is drawn with.
    public static string ModelOf(Items.ItemType type)
    {
        switch (type)
        {
            case Items.ItemType.PottedMonstera:
                return "monstera_plant_small_potted";
            case Items.ItemType.PottedPothos:
                return "pothos_plant_small_potted";
            case Items.ItemType.PottedSnakePlant:
                return "sansevieria_plant_small_potted";
            case Items.ItemType.PottedYucca:
                return "yucca_plant_small_potted";
            default:
                return "zzplant_plant_small_potted";
        }
    }
}
