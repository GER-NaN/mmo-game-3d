namespace MmoGame3d.Rules.Players;

/// <summary>
/// What a player can look like: a base character, one of the KayKit collection's (see
/// assets/README.md). The model of each look is the client's business, so only the id is
/// kept and synced; a base's id is its model's file name, but for the two Protagonists,
/// whose ids "a" and "b" came first.
/// </summary>
public static class Looks
{
    public const string Default = "a";

    // In the order the character creator shows them.
    private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
    {
        { "a", "Protagonist A" },
        { "b", "Protagonist B" },
        { "4GTN", "4GTN" },
        { "4GTN_Forgotten", "4GTN Forgotten" },
        { "ActionFigure", "Action Figure" },
        { "Animatronic_Creepy", "Animatronic Creepy" },
        { "Animatronic_Normal", "Animatronic Normal" },
        { "AvianSwordsman", "Avian Swordsman" },
        { "Barbarian", "Barbarian" },
        { "Barbarian_Large", "Barbarian Large" },
        { "BlackKnight", "Black Knight" },
        { "Caveman", "Caveman" },
        { "Clanker", "Clanker" },
        { "Cleric", "Cleric" },
        { "Clown", "Clown" },
        { "CombatMech", "Combat Mech" },
        { "Driver", "Driver" },
        { "Druid", "Druid" },
        { "Dummy", "Dummy" },
        { "Engineer", "Engineer" },
        { "Farmer_A", "Farmer A" },
        { "Farmer_B", "Farmer B" },
        { "FrostGolem", "Frost Golem" },
        { "Helper_A", "Helper A" },
        { "Helper_B", "Helper B" },
        { "Hiker", "Hiker" },
        { "Hoarder", "Hoarder" },
        { "Knight", "Knight" },
        { "Lorekeeper", "Lorekeeper" },
        { "Mage", "Mage" },
        { "MagicalGirl", "Magical Girl" },
        { "Mannequin_Large", "Mannequin Large" },
        { "Mannequin_Medium", "Mannequin Medium" },
        { "Marksman", "Marksman" },
        { "Monster", "Monster" },
        { "MonsterCostume", "Monster Costume" },
        { "Monstrosity", "Monstrosity" },
        { "Necromancer", "Necromancer" },
        { "Ninja", "Ninja" },
        { "OrcBrute", "Orc Brute" },
        { "OrcRaider", "Orc Raider" },
        { "Paladin", "Paladin" },
        { "Paladin_with_Helmet", "Paladin with Helmet" },
        { "PlantWarrior", "Plant Warrior" },
        { "Ranger", "Ranger" },
        { "Robot_One", "Robot One" },
        { "Robot_Two", "Robot Two" },
        { "Rogue", "Rogue" },
        { "Rogue_Hooded", "Rogue Hooded" },
        { "Skeleton_Golem", "Skeleton Golem" },
        { "Skeleton_Mage", "Skeleton Mage" },
        { "Skeleton_Minion", "Skeleton Minion" },
        { "Skeleton_Rogue", "Skeleton Rogue" },
        { "Skeleton_Warrior", "Skeleton Warrior" },
        { "SpaceRanger", "Space Ranger" },
        { "SpaceRanger_FlightMode", "Space Ranger Flight Mode" },
        { "Superhero", "Superhero" },
        { "Survivalist", "Survivalist" },
        { "Tiefling", "Tiefling" },
        { "ToySoldier", "Toy Soldier" },
        { "Vampire", "Vampire" },
        { "Werewolf_Man", "Werewolf Man" },
        { "Werewolf_Wolf", "Werewolf Wolf" },
        { "Witch", "Witch" },
    };

    public static IEnumerable<string> Ids
    {
        get { return Names.Keys; }
    }

    public static bool IsKnown(string id)
    {
        return Names.ContainsKey(id);
    }

    public static string NameOf(string id)
    {
        string? name;
        return Names.TryGetValue(id, out name) ? name : Names[Default];
    }

    // A look this build does not know (a newer client, a bad row) shows as the default.
    public static string OrDefault(string id)
    {
        return IsKnown(id) ? id : Default;
    }
}
