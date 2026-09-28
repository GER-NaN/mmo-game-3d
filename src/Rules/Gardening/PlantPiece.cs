namespace MmoGame3d.Rules.Gardening;
/// <summary>
/// One piece of a plant: where it stands on the soil, as a fraction of the soil's
/// radius (so a design survives a change of pot size), how it is turned (yaw), leaned
/// out (tilt) and sized.
/// </summary>
public class PlantPiece
{
    public const float MaxTiltRadians = 0.8f;
    public const float MinScale = 0.6f;
    public const float MaxScale = 1.4f;

    public string Id { get; set; } = "";
    public float X { get; set; }
    public float Z { get; set; }
    public float Yaw { get; set; }
    public float Tilt { get; set; }
    public float Scale { get; set; } = 1f;
}
