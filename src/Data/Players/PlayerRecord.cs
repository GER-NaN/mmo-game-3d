namespace MmoGame3d.Data.Players;

using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Social;

/// <summary>
/// A player as it is kept between sessions. A record handed to the persistence worker
/// is a copy the game thread no longer touches.
/// </summary>
public class PlayerRecord
{
    // What they carry: counts of identical things, and things with an identity.
    public List<ItemStack> Stacks { get; set; } = new List<ItemStack>();
    public List<ItemInstance> Instances { get; set; } = new List<ItemInstance>();

    // Where they have been, per zone (see Discovery). Loaded and saved by DiscoveryStore,
    // not by PlayerStore.
    public Dictionary<string, byte[]> Discovered { get; set; } = new Dictionary<string, byte[]>();

    // Friends and ignores. Loaded by ContactStore, not by PlayerStore, and written by it
    // on each change rather than with the player.
    public Contacts Contacts { get; set; } = new Contacts();

    // True when GetOrCreate made this player just now. Not stored.
    public bool Created { get; set; }

    public Guid PlayerId { get; set; }
    public Guid AccountId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Zone { get; set; } = "";
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float Yaw { get; set; }
    public int Dollars { get; set; }

    // Chosen once, at creation; see Looks.
    public string Look { get; set; } = "a";
}
