namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Drones;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Items;
using MmoGame3d.Zones;

/// <summary>
/// Drones over the town, and the EMP Emitter that brings them down. A pair spawns
/// together whenever the town has none, circling a spot somewhere over the streets. An
/// EMP pulse knocks out every drone within range of the player who fires it; a downed
/// drone falls, lies a few seconds, and is gone.
/// </summary>
public class ServerDrones
{
    // Tells the achievements when one is earned here; set by ServerGame.
    public Action<Session, string>? Achieved { get; set; }

    private static readonly PackedScene DroneScene = GD.Load<PackedScene>("res://game/drones/Drone.tscn");

    // Placeholders.
    private const double SpawnCheckSeconds = 20;
    private const float SpawnArea = 30f;
    private const float EmpRange = 10f;
    private const double EmpCooldownSeconds = 2;
    private const double LieSeconds = 4;
    private const double ZapEverySeconds = 4;
    private const float ZapRange = 7f;
    private const int ZapDamage = 10;
    private const float KeepFromSpawn = 15f;

    private readonly Zone _zone;
    private readonly VisibilityGate _gate;
    private readonly Network _session;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly Random _random = new Random();
    private readonly Dictionary<Session, double> _lastPulse = new Dictionary<Session, double>();
    private double _sinceCheck = SpawnCheckSeconds;
    private double _clock;
    private double _sinceZap;
    private int _spawned;

    // Raised for a player a drone hurt: (who, how much).
    // What happens here, for the terminal's status board; set by ServerGame.
    public Action<string>? Post { get; set; }

    public event Action<Session, int>? Hurt;

    public ServerDrones(Zone zone, VisibilityGate gate, Network session, Func<IEnumerable<Session>> sessions)
    {
        _zone = zone;
        _gate = gate;
        _session = session;
        _sessions = sessions;
    }

    public void Tick(double delta)
    {
        _clock += delta;
        _sinceCheck += delta;
        Node3D drones = _zone.GetNode<Node3D>("Drones");

        foreach (Node node in drones.GetChildren())
        {
            Drone? drone = node as Drone;

            if (drone != null && drone.Down && drone.LyingFor >= LieSeconds && !drone.IsQueuedForDeletion())
            {
                drone.QueueFree();
            }
        }

        _sinceZap += delta;

        if (_sinceZap >= ZapEverySeconds)
        {
            _sinceZap = 0;

            foreach (Node node in drones.GetChildren())
            {
                Drone? drone = node as Drone;

                if (drone != null && !drone.Down)
                {
                    Zap(drone);
                }
            }
        }

        if (_sinceCheck < SpawnCheckSeconds || drones.GetChildCount() > 0)
        {
            return;
        }

        _sinceCheck = 0;
        Vector3 center = Vector3.Zero;

        // Not over the spawn: a player who fainted wakes there.
        for (int tries = 0; tries < 20; tries++)
        {
            center = new Vector3(
                (float)((_random.NextDouble() * 2) - 1) * SpawnArea,
                0f,
                (float)((_random.NextDouble() * 2) - 1) * SpawnArea);

            if (center.DistanceTo(_zone.SpawnPoint) >= KeepFromSpawn)
            {
                break;
            }
        }

        for (int i = 0; i < 2; i++)
        {
            Drone drone = DroneScene.Instantiate<Drone>();
            _spawned++;
            drone.Name = "Drone" + _spawned;
            drone.Center = center;
            drone.Phase = i * Mathf.Pi;
            drone.NetPosition = center + new Vector3(0f, Drone.Height, 0f);
            _gate.Watch(drone.Synchronizer, _zone.ZoneId);
            drones.AddChild(drone, true);
        }

        GD.Print("Two drones are up over town, around " + center);
        Post?.Invoke("Two drones are up over Old Town.");
    }

    public void Fire(Session session)
    {
        if (session.Body == null || session.Inventory == null)
        {
            return;
        }

        Belongings mine = new Belongings(session.Inventory, session.Instances);
        ItemInstance? tool = mine.Equipped(SlotType.Tool);

        if (tool == null || tool.Type != ItemType.EmpEmitter)
        {
            _session.SendNotice(session.PeerId, "You have no EMP Emitter equipped. The electronics shop sells them.");
            return;
        }

        double last;

        if (_lastPulse.TryGetValue(session, out last) && _clock - last < EmpCooldownSeconds)
        {
            return;
        }

        _lastPulse[session] = _clock;
        Vector3 at = session.Body.Position;

        // The pulse is seen by everyone in the zone, the downing only where the drones are.
        foreach (Session other in _sessions())
        {
            if (other.State == SessionState.InWorld && other.ZoneId == session.ZoneId)
            {
                _session.SendEmpPulse(other.PeerId, at);
            }
        }

        if (session.ZoneId != _zone.ZoneId)
        {
            return;
        }

        int downed = 0;

        foreach (Node node in _zone.GetNode("Drones").GetChildren())
        {
            Drone? drone = node as Drone;

            if (drone != null && !drone.Down && drone.NetPosition.DistanceTo(at) <= EmpRange)
            {
                drone.Down = true;
                downed++;
            }
        }

        if (downed > 0)
        {
            _session.SendNotice(session.PeerId, downed == 1 ? "A drone drops out of the sky." : downed + " drones drop out of the sky.");
            Achieved?.Invoke(session, Rules.Achievements.Achievements.DownADrone);
            Post?.Invoke(session.Record!.DisplayName + " brought down " + (downed == 1 ? "a drone" : downed + " drones") + " with an EMP.");
        }
    }

    // The nearest player in range. An online player's body is as open to it as any
    // (world.md 8: online, your body is vulnerable).
    private void Zap(Drone drone)
    {
        Session? target = null;
        float nearest = ZapRange;

        foreach (Session session in _sessions())
        {
            if (session.State != SessionState.InWorld || session.Body == null || session.ZoneId != _zone.ZoneId)
            {
                continue;
            }

            Vector3 feet = session.Body.Position;
            float distance = new Vector2(feet.X - drone.NetPosition.X, feet.Z - drone.NetPosition.Z).Length();

            if (distance <= nearest)
            {
                nearest = distance;
                target = session;
            }
        }

        if (target == null)
        {
            return;
        }

        Vector3 to = target.Body!.Position + new Vector3(0f, 1f, 0f);

        foreach (Session other in _sessions())
        {
            if (other.State == SessionState.InWorld && other.ZoneId == _zone.ZoneId)
            {
                _session.SendDroneZap(other.PeerId, drone.NetPosition, to);
            }
        }

        Hurt?.Invoke(target, ZapDamage);
    }

    public void Forget(Session session)
    {
        _lastPulse.Remove(session);
    }
}
