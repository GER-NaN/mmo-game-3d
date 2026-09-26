namespace MmoGame3d.Rules.Players;

/// <summary>
/// What a player can look like. Chosen once, when the player is made, like the name; the
/// model of each look is the client's business, so only the id is kept and synced.
/// Placeholder choices until character creation is designed.
/// </summary>
public static class Looks
{
    public const string Default = "a";

    private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
    {
        { "a", "Protagonist A" },
        { "b", "Protagonist B" },
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
