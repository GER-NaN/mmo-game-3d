namespace MmoGame3d.Data.Players;

/// <summary>
/// A player as it is kept between sessions.
/// </summary>
public class PlayerRecord
{
    public Guid PlayerId { get; set; }
    public Guid AccountId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Zone { get; set; } = "";
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float Yaw { get; set; }
}
