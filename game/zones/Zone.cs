namespace MmoGame3d.Zones;

using Godot;

/// <summary>
/// One place in the world, built in the editor as a scene. The server moves bodies in
/// it with the same collision the client draws, so there is no separate map data.
///
/// A zone scene needs: a "Spawn" marker where new players appear; "Players" and
/// "Items" nodes, each with a spawner pointing at it; and everything that blocks on the
/// World physics layer.
/// </summary>
public partial class Zone : Node3D
{
    // Ground items lie within this box around the zone's origin, in world units.
    [Export]
    public Vector2 ItemAreaSize { get; set; } = new Vector2(50f, 50f);

    // How many ground items the zone keeps topped up to. Placeholder density.
    [Export]
    public int ItemStock { get; set; } = 15;

    public string ZoneId
    {
        get { return Name; }
    }

    public Node3D Players
    {
        get { return GetNode<Node3D>("Players"); }
    }

    public Node3D Items
    {
        get { return GetNode<Node3D>("Items"); }
    }

    public Vector3 SpawnPoint
    {
        get { return GetNode<Node3D>("Spawn").GlobalPosition; }
    }
}
