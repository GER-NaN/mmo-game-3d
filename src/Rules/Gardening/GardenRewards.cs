namespace MmoGame3d.Rules.Gardening;
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
