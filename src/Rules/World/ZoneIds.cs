namespace MmoGame3d.Rules.World;

// The zones the server loads. A zone id is also its node name in the scene tree, on
// the server and on the client, since synced nodes are found by path.
public static class ZoneIds
{
    public const string Town = "town";

    public const string Start = Town;

    public static readonly string[] All = { Town };
}
