namespace MmoGame3d.Rules.Items;

// Stored by name in the database, so a new type goes at the end or anywhere, but a name
// never changes once players hold it.
public enum ItemType
{
    GpuCore,
    RamStick,
    Phone,
    Battery,
    EmpEmitter,

    // Small ready-made house plants, the greenhouse's reward.
    PottedMonstera,
    PottedPothos,
    PottedSnakePlant,
    PottedYucca,
    PottedZzPlant,
}

// Quality, low to high. Placeholder names.
public enum ItemTier
{
    Standard,
    Enhanced,
    Advanced,
    Elite,
}
