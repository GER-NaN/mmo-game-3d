namespace MmoGame3d.Rules.Town;
public class SubwayTag
{
    public long Id { get; set; }
    public string Name { get; set; } = "";

    // RGBA, as Godot's Color(uint) reads it.
    public uint Paint { get; set; }
}
