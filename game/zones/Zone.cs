namespace MmoGame3d.Zones;

using Godot;

/// <summary>
/// One place in the world, built in the editor as a scene. The server moves bodies in
/// it with the same collision the client draws, so there is no separate map data.
///
/// A zone scene needs: a "Spawn" marker where new players appear; "Players" and
/// "Items" nodes, each with a spawner pointing at it; everything that blocks on the
/// World physics layer; and, for doors in, markers under "Arrivals".
/// </summary>
public partial class Zone : Node3D
{
    // Ground items lie within this box around the zone's origin, in world units.
    [Export]
    public Vector2 ItemAreaSize { get; set; } = new Vector2(50f, 50f);

    // How many ground items the zone keeps topped up to. Placeholder density.
    [Export]
    public int ItemStock { get; set; } = 15;

    // The ground the map covers, centred on the zone's origin. Zero means the zone has no
    // map: an interior or a ride, where there is nothing to find your way around.
    [Export]
    public Vector2 MapSize { get; set; } = Vector2.Zero;

    // What the ground mostly is, for footsteps: the catalog's "step.<surface>". Empty
    // is silent (a ride).
    [Export]
    public string Surface { get; set; } = "rock";

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

    // Positions here are zone-local: the server lays its zones out far apart, while a
    // client loads its one zone at the origin, so only zone-local numbers mean the same
    // on both sides. Synced positions are zone-local for the same reason.
    public Vector3 SpawnPoint
    {
        get { return GetNode<Node3D>("Spawn").Position; }
    }

    // Where a door into this zone puts you: a marker under "Arrivals", or null.
    public Node3D? Arrival(string name)
    {
        return GetNodeOrNull<Node3D>("Arrivals/" + name);
    }
}
