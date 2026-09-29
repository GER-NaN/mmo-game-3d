namespace MmoGame3d.Rules.Town;
public class SubwayTag
{
    public long Id { get; set; }
    public string Name { get; set; } = "";

    // RGBA, as Godot's Color(uint) reads it.
    public uint Paint { get; set; }

    // Null until the server has placed it (tags sprayed before places were kept).
    public TagPlace? Place { get; set; }
}
