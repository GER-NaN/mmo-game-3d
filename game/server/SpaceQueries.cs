namespace MmoGame3d.Server;

using Godot;
using MmoGame3d.Zones;

/// <summary>
/// Physics questions about where a body can stand. Only the world counts: players pass
/// through each other.
/// </summary>
public static class SpaceQueries
{
    // A player-sized capsule, lifted a little so the floor it stands on does not count.
    private static readonly Vector3 Lift = new Vector3(0f, 1.05f, 0f);

    // feet is a global position: physics works in the one space all zones share.
    public static bool IsFree(Zone zone, Vector3 feet)
    {
        PhysicsShapeQueryParameters3D query = new PhysicsShapeQueryParameters3D
        {
            Shape = new CapsuleShape3D(),
            CollisionMask = PhysicsLayers.World,
            Transform = new Transform3D(Basis.Identity, feet + Lift),
        };

        return zone.GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    // The wanted spot, or the nearest free spot on rings around it; both zone-local. A
    // saved spot can be inside something built since.
    public static Vector3 FreeSpotNear(Zone zone, Vector3 wanted)
    {
        const float RingStep = 1.5f;
        const int Rings = 4;
        const int SpotsPerRing = 8;

        for (int ring = 0; ring <= Rings; ring++)
        {
            int spots = ring == 0 ? 1 : SpotsPerRing;

            for (int spot = 0; spot < spots; spot++)
            {
                float angle = Mathf.Tau * spot / spots;
                Vector3 candidate = wanted + (new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * RingStep));

                if (IsFree(zone, zone.ToGlobal(candidate)))
                {
                    return candidate;
                }
            }
        }

        return wanted;
    }
}
