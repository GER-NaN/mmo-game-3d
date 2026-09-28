namespace MmoGame3d.Dev;
/// <summary>A door out of a zone, and the zone it leads to.</summary>
public sealed class DoorLink
{
    public DoorLink(string door, string target)
    {
        Door = door;
        Target = target;
    }

    public string Door { get; }

    public string Target { get; }
}
