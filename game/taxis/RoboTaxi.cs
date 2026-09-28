namespace MmoGame3d.Taxis;

using Godot;

/// <summary>
/// A robo taxi driving its route through town while its riders sit in the cabin
/// instance. Only how far along the route it is gets synced; every client has the same
/// route in its town scene, so it places the car on the route itself and drives it on
/// at the taxi's speed between updates, which keeps it smooth.
/// </summary>
public partial class RoboTaxi : Node3D
{
    // Placeholder: slow enough to watch go by, a ride around town takes about a minute.
    public const float Speed = 6f;

    // How far a client's car may lag or lead the server's before it jumps.
    private const float SnapDistance = 5f;

    private float _shown;
    private bool _placed;

    // Metres along the route, set by the server.
    [Export]
    public float Progress { get; set; }

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    // The route is set on the zone this car is spawned in.
    public Path3D Route
    {
        get
        {
            Node? node = GetParent();

            while (node != null && !(node is Zones.Zone))
            {
                node = node.GetParent();
            }

            return ((Zones.Zone)node!).TaxiRoute!;
        }
    }

    public float RouteLength
    {
        get { return Route.Curve.GetBakedLength(); }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            _shown = Progress;
        }
        else if (!_placed || Mathf.Abs(Progress - _shown) > SnapDistance)
        {
            _shown = Progress;
            _placed = true;
        }
        else
        {
            // Drive on at speed, and lean gently towards the server's number.
            _shown = Mathf.Lerp(_shown + (Speed * (float)delta), Progress, 0.05f);
        }

        Path3D route = Route;
        float along = Mathf.Clamp(_shown, 0f, route.Curve.GetBakedLength());
        Transform = route.Transform * route.Curve.SampleBakedWithRotation(along, false, false);
    }
}
