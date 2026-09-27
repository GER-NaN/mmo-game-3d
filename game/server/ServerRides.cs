namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Rules.World;
using MmoGame3d.Taxis;
using MmoGame3d.Zones;

/// <summary>
/// Robo taxi rides. Calling a taxi makes a ride: its own cabin instance (a zone made for
/// this ride alone, "taxi-3"), and its own car driving the town's taxi route, which
/// everyone in town sees. The caller rides, with their party if they are close by, the
/// way a door takes a party. In the cabin they can do anything they do elsewhere: look
/// round, use their bag, go online on their phone. When the car reaches the end of its
/// route, everyone still in the cabin is put out at the drop-off, and the ride ends.
/// </summary>
public class ServerRides
{
    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    // Party members this close to whoever calls the taxi ride along.
    private const float PartyDistance = 10f;

    private static readonly PackedScene CarScene = GD.Load<PackedScene>("res://game/taxis/RoboTaxi.tscn");

    private readonly World _world;
    private readonly VisibilityGate _gate;
    private readonly Network _session;
    private readonly ServerParties _parties;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly Action<List<Session>, Zone, Node3D, string> _travel;
    private readonly List<Ride> _rides = new List<Ride>();
    private int _nextRide = 1;

    public ServerRides(World world, VisibilityGate gate, Network session, ServerParties parties, Func<IEnumerable<Session>> sessions, Action<List<Session>, Zone, Node3D, string> travel)
    {
        _world = world;
        _gate = gate;
        _session = session;
        _parties = parties;
        _sessions = sessions;
        _travel = travel;
    }

    // False while the AI's rootkit is in the taxis; set by ServerGame.
    public Func<bool> TaxisClean { get; set; } = () => true;

    public void Call(Session caller, TaxiStand stand)
    {
        if (!TaxisClean())
        {
            _session.SendNotice(caller.PeerId, "The robo taxis are out of service: the AI put a rootkit in them. Town repairs in a terminal has the job.");
            return;
        }

        Zone town = _world.GetZone(ZoneIds.Town)!;
        int number = _nextRide++;
        string cabinId = ZoneIds.Instance(ZoneIds.Taxi, number);
        Zone cabin = _world.LoadZone(cabinId);

        TaxiState state = cabin.GetNode<TaxiState>("TaxiState");
        _gate.Watch(state.Synchronizer, cabinId);

        RoboTaxi car = CarScene.Instantiate<RoboTaxi>();
        car.Name = "Taxi" + number;
        _gate.Watch(car.Synchronizer, ZoneIds.Town);
        town.GetNode<Node3D>("Vehicles").AddChild(car, true);

        Ride ride = new Ride(cabinId, cabin, car, state, car.RouteLength);
        state.SecondsLeft = ride.Length / RoboTaxi.Speed;
        _rides.Add(ride);

        List<Session> riders = new List<Session> { caller };
        Vector3 at = caller.Body!.GlobalPosition;

        foreach (Session other in _parties.OthersOnline(caller))
        {
            if (other.Record!.Zone == caller.Record!.Zone && other.Body != null && other.Body.GlobalPosition.DistanceTo(at) <= PartyDistance)
            {
                riders.Add(other);
            }
        }

        _travel(riders, cabin, cabin.Arrival("Seat")!, "You rode along with your party.");

        foreach (Session rider in riders)
        {
            Achieved?.Invoke(rider, Rules.Achievements.Achievements.RoboTaxi);
            _session.SendNotice(rider.PeerId, "The robo taxi drives you round town. About " + Mathf.RoundToInt(state.SecondsLeft) + " seconds to the drop-off.");
        }

        GD.Print("Ride " + cabinId + " started for " + riders.Count + " rider(s)");
    }

    public void Tick(double delta)
    {
        for (int i = _rides.Count - 1; i >= 0; i--)
        {
            Ride ride = _rides[i];
            ride.Car.Progress = Mathf.Min(ride.Length, ride.Car.Progress + (RoboTaxi.Speed * (float)delta));
            ride.State.SecondsLeft = (ride.Length - ride.Car.Progress) / RoboTaxi.Speed;

            if (ride.Car.Progress >= ride.Length)
            {
                End(ride);
                _rides.RemoveAt(i);
            }
        }
    }

    // Everyone still in the cabin, landed or still loading it, is put out at the drop-off.
    private void End(Ride ride)
    {
        Zone town = _world.GetZone(ZoneIds.Town)!;
        List<Session> riders = new List<Session>();

        foreach (Session session in _sessions())
        {
            if (session.Record != null && session.Record.Zone == ride.CabinId
                && (session.State == SessionState.InWorld || session.State == SessionState.Accepted))
            {
                riders.Add(session);
            }
        }

        if (riders.Count > 0)
        {
            _travel(riders, town, town.Arrival("TaxiDropOff")!, "");

            foreach (Session rider in riders)
            {
                _session.SendNotice(rider.PeerId, "You have arrived. The robo taxi drives off.");
            }
        }

        ride.Car.QueueFree();
        _world.UnloadZone(ride.CabinId);
        GD.Print("Ride " + ride.CabinId + " ended; " + riders.Count + " rider(s) put out");
    }

    private class Ride
    {
        public Ride(string cabinId, Zone cabin, RoboTaxi car, TaxiState state, float length)
        {
            CabinId = cabinId;
            Cabin = cabin;
            Car = car;
            State = state;
            Length = length;
        }

        public string CabinId { get; }
        public Zone Cabin { get; }
        public RoboTaxi Car { get; }
        public TaxiState State { get; }
        public float Length { get; }
    }
}
