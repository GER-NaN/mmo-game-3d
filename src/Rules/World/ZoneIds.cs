namespace MmoGame3d.Rules.World;

// The zones there are. A zone id is also its node name in the scene tree, on the server
// and on the client, since synced nodes are found by path.
//
// Most zones are loaded once, at start, and the id is the scene's name. An instance is a
// zone made for one use (a taxi ride): its id is the scene's name, a dash and a number,
// so both sides load the same scene under the same unique name.
public static class ZoneIds
{
    public const string Town = "town";
    public const string Outskirts = "outskirts";
    public const string Shop = "shop";
    public const string Taxi = "taxi";
    public const string College = "college";

    public const string Start = Town;

    // Loaded at start. Instance scenes (the taxi) are not: they are made per use.
    public static readonly string[] All = { Town, Outskirts, Shop, College };

    public static string Instance(string scene, int number)
    {
        return scene + "-" + number;
    }

    // The scene a zone is made from: the id itself, or the part before an instance number.
    public static string SceneOf(string zoneId)
    {
        int dash = zoneId.LastIndexOf('-');
        int number;

        if (dash > 0 && int.TryParse(zoneId.Substring(dash + 1), out number))
        {
            return zoneId.Substring(0, dash);
        }

        return zoneId;
    }
}
