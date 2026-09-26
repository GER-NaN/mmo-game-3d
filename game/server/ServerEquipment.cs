namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Social;
using MmoGame3d.Workbenches;

/// <summary>
/// Equipment on the server: equipping and unequipping, the workbench, going online by
/// phone, and phone batteries running down. The rules are in Belongings and Power; this
/// applies them to sessions, sends the owner their new bag, and checks the workbench is
/// in reach, since a client could ask for a swap from anywhere.
/// </summary>
public class ServerEquipment
{
    // Batteries are drained once a second; the charge is a real-time thing.
    private const double DrainIntervalSeconds = 1;
    private const float ReachSlack = 1.5f;

    private readonly ItemNetwork _network;
    private readonly Network _session;
    private readonly ServerTerminals _terminals;
    private readonly Func<IEnumerable<Session>> _sessions;
    private readonly Action<Session> _bagChanged;
    private double _sinceDrain;

    public ServerEquipment(ItemNetwork network, Network session, ServerTerminals terminals, Func<IEnumerable<Session>> sessions, Action<Session> bagChanged)
    {
        _network = network;
        _session = session;
        _terminals = terminals;
        _sessions = sessions;
        _bagChanged = bagChanged;
    }

    public void Equip(Session session, string instanceId)
    {
        Apply(session, instanceId, (mine, id) => mine.Equip(id));
    }

    public void Unequip(Session session, string instanceId)
    {
        // Taking the phone off while online on it takes you offline first.
        if (_terminals.IsOnPhone(session))
        {
            _terminals.Leave(session);
        }

        Apply(session, instanceId, (mine, id) => mine.Unequip(id));
    }

    public void OpenWorkbench(Session session, Workbench workbench)
    {
        session.OpenWorkbench = workbench;
        _network.SendWorkbenchOpened(session.PeerId);
    }

    public void RemoveBattery(Session session, string phoneId)
    {
        if (AtWorkbench(session))
        {
            Apply(session, phoneId, (mine, id) => mine.RemoveBattery(id));
            session.Body?.Show(Gestures.Work);
        }
    }

    public void InsertBattery(Session session, string phoneId)
    {
        if (AtWorkbench(session))
        {
            Apply(session, phoneId, (mine, id) => mine.InsertBattery(id));
            session.Body?.Show(Gestures.Work);
        }
    }

    public void UsePhone(Session session)
    {
        string? refusal = Power.CannotGoOnline(Belongings(session));

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        _terminals.UsePhone(session);
    }

    public void Tick(double delta)
    {
        _sinceDrain += delta;

        if (_sinceDrain < DrainIntervalSeconds)
        {
            return;
        }

        float seconds = (float)_sinceDrain;
        _sinceDrain = 0;

        foreach (Session session in _sessions())
        {
            if (session.State != SessionState.InWorld || session.Inventory == null)
            {
                continue;
            }

            ItemInstance? battery = Belongings(session).DeviceBattery();

            if (battery == null || battery.Charge == null || battery.Charge <= 0f)
            {
                continue;
            }

            bool onPhone = _terminals.IsOnPhone(session);
            int before = Power.Percent(battery.Charge);
            battery.Charge = Power.Drain(battery.Charge.Value, seconds, onPhone);

            if (battery.Charge <= 0f && onPhone)
            {
                _terminals.Leave(session);
                _session.SendNotice(session.PeerId, "Your phone's battery died. You are offline.");
            }

            // A whole percent is worth telling the owner; every second would not be.
            if (Power.Percent(battery.Charge) != before)
            {
                _bagChanged(session);
            }
        }
    }

    private bool AtWorkbench(Session session)
    {
        Workbench? bench = session.OpenWorkbench;
        bool atBench = bench != null && GodotObject.IsInstanceValid(bench) && session.Body != null
            && bench.IsInReach(session.Body.GlobalPosition, ReachSlack);

        if (!atBench)
        {
            _session.SendNotice(session.PeerId, "You need to be at a workbench for that.");
        }

        return atBench;
    }

    private void Apply(Session session, string instanceId, Func<Belongings, Guid, string?> change)
    {
        Guid id;

        if (session.Inventory == null || !Guid.TryParse(instanceId, out id))
        {
            return;
        }

        string? refusal = change(Belongings(session), id);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        _bagChanged(session);
    }

    private static Belongings Belongings(Session session)
    {
        return new Belongings(session.Inventory!, session.Instances);
    }
}
