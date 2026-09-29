namespace MmoGame3d.Rules.Town;

// Where a tag is on its wall: the centre, in metres across (0 is the middle) and up
// the wall's face; its slant in degrees; its font size.
public class TagPlace
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
    public int Size { get; set; }
}
