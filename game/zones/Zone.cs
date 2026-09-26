namespace MmoGame3d.Zones;

using Godot;

/// <summary>
/// One place in the world, built in the editor as a scene. The server moves bodies in
/// it with the same collision the client draws, so there is no separate map data.
/// A zone scene must have a "Players" node (with a spawner pointing at it) and a
/// "Spawn" marker where new players appear.
/// </summary>
public partial class Zone : Node3D
{
    public string ZoneId
    {
        get { return Name; }
    }

    public Node3D Players
    {
        get { return GetNode<Node3D>("Players"); }
    }

    public Vector3 SpawnPoint
    {
        get { return GetNode<Node3D>("Spawn").GlobalPosition; }
    }
}
