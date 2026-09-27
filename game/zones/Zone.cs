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

    // Set by World when it loads the zone; the node itself is always named "Zone".
    public string ZoneId { get; set; } = "";

    public Node3D Players
    {
        get { return GetNode<Node3D>("Players"); }
    }

    public Node3D Items
    {
        get { return GetNode<Node3D>("Items"); }
    }

    // Positions here are zone-local. Every zone sits at the origin of its own space, so
    // today zone-local and global are the same; zone-local keeps it true if that changes.
    public Vector3 SpawnPoint
    {
        get { return GetNode<Node3D>("Spawn").Position; }
    }

    // Where a door into this zone puts you: a marker under "Arrivals", or null.
    public Node3D? Arrival(string name)
    {
        return GetNodeOrNull<Node3D>("Arrivals/" + name);
    }

    // Terrain3D's collision modes (Terrain3DCollision): it is a GDExtension, so C# reaches
    // it by name, without generated types.
    private const int TerrainCollisionFull = 3;

    public override void _Ready()
    {
        Node? terrain = GetNodeOrNull("Terrain");

        if (terrain == null || !terrain.IsClass("Terrain3D"))
        {
            return;
        }

        // The ground blocks players and the chase camera, as any wall does. Both sides build
        // all of it as the zone loads, so it is there before a player is placed: built round
        // the camera instead (the scene's default), a client's arriving player fell through
        // ground that did not exist yet, and was snapped back up by the server.
        GodotObject collision = terrain.Get("collision").AsGodotObject();
        collision.Set("layer", PhysicsLayers.World | PhysicsLayers.CameraBlock);
        collision.Set("mode", TerrainCollisionFull);

        if (!Multiplayer.IsServer())
        {
            return;
        }

        // The server has no camera; Terrain3D stops its processing without one.
        Camera3D stand = new Camera3D { Name = "TerrainCamera" };
        AddChild(stand);
        terrain.Call("set_camera", stand);
    }
}
