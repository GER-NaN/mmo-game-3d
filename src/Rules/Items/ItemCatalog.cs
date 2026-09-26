namespace MmoGame3d.Rules.Items;

/// <summary>
/// What each kind of item is. Both sides build the same catalog from this code, so a
/// definition never has to travel over the network: client and server are one build.
/// </summary>
public static class ItemCatalog
{
    private static readonly Dictionary<ItemType, ItemDefinition> Definitions = new Dictionary<ItemType, ItemDefinition>
    {
        {
            ItemType.GpuCore,
            new ItemDefinition(ItemType.GpuCore, "GPU core", "Compute. Spend it, trade it, or put it to work.", true)
        },
        {
            ItemType.RamStick,
            new ItemDefinition(ItemType.RamStick, "RAM stick", "Memory. A part for building and repairing.", true)
        },
        {
            ItemType.Phone,
            new ItemDefinition(ItemType.Phone, "Phone", "A door into the terminal world you can carry. Needs a battery.", false)
        },
        {
            ItemType.Battery,
            new ItemDefinition(ItemType.Battery, "Battery", "Fits a phone. Swap it at a workbench.", true)
        },
        {
            ItemType.EmpEmitter,
            new ItemDefinition(ItemType.EmpEmitter, "EMP Emitter", "A pulse that knocks drones out of the sky. Equip it, then press R near one.", false)
        },
        { ItemType.PottedMonstera, new ItemDefinition(ItemType.PottedMonstera, "Small potted monstera", "A little house plant from the greenhouse.", true) },
        { ItemType.PottedPothos, new ItemDefinition(ItemType.PottedPothos, "Small potted pothos", "A little house plant from the greenhouse.", true) },
        { ItemType.PottedSnakePlant, new ItemDefinition(ItemType.PottedSnakePlant, "Small potted snake plant", "A little house plant from the greenhouse.", true) },
        { ItemType.PottedYucca, new ItemDefinition(ItemType.PottedYucca, "Small potted yucca", "A little house plant from the greenhouse.", true) },
        { ItemType.PottedZzPlant, new ItemDefinition(ItemType.PottedZzPlant, "Small potted ZZ plant", "A little house plant from the greenhouse.", true) },
    };

    public static ItemDefinition Get(ItemType type)
    {
        return Definitions[type];
    }

    public static string Describe(ItemType type, ItemTier tier)
    {
        return tier == ItemTier.Standard ? Get(type).Name : tier + " " + Get(type).Name;
    }
}

public class ItemDefinition
{
    public ItemDefinition(ItemType type, string name, string description, bool stackable)
    {
        Type = type;
        Name = name;
        Description = description;
        Stackable = stackable;
    }

    public ItemType Type { get; }
    public string Name { get; }
    public string Description { get; }

    // A stack is a count of identical things; a thing that is not stackable has an
    // identity and state of its own (this phone), and comes with equipment.
    public bool Stackable { get; }
}
