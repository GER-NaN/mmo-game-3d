namespace MmoGame3d;

// Physics layers as bit masks. Players are on their own layer and collide only with the
// world: two bodies that overlap push each other apart every frame, hard enough to
// launch both into the sky, and an MMO lets people walk through each other anyway.
public static class PhysicsLayers
{
    public const uint World = 1;
    public const uint Players = 2;
}
