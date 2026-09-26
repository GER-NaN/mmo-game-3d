namespace MmoGame3d.Data.Players;

using MmoGame3d.Rules.Items;

/// <summary>
/// A player as it is kept between sessions. A record handed to the persistence worker
/// is a copy the game thread no longer touches.
/// </summary>
public class PlayerRecord
{
    // What they carry.
    public List<ItemStack> Stacks { get; set; } = new List<ItemStack>();

    public Guid PlayerId { get; set; }
    public Guid AccountId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Zone { get; set; } = "";
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float Yaw { get; set; }
    public int Dollars { get; set; }
}
