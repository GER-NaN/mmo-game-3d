namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Terminals;

/// <summary>
/// Going online and offline at fixed terminals, and the roster of who is online. The
/// rules live in TerminalAccess; this keeps the terminal nodes, the bodies and the
/// screens in step with it. The terminal world is not split by zone: the roster lists
/// everyone in the world, wherever they stand.
/// </summary>
public class ServerTerminals
{
    // How often the online get the roster. It is small, and a join shows within this.
    private const double RosterIntervalSeconds = 2;

    private readonly TerminalAccess _access = new TerminalAccess();
    private readonly StatusFeed _status = new StatusFeed();
    private readonly Dictionary<string, Terminal> _terminalsByKey = new Dictionary<string, Terminal>();
    private readonly HashSet<Guid> _onPhone = new HashSet<Guid>();
    private readonly TerminalNetwork _network;
    private readonly Network _session;
    private readonly Func<IEnumerable<Session>> _sessions;
    private double _sinceRoster;
    private string _rosterKey = "";

    // The world clock as "hh:mm", for the status board.
    public Func<string> Clock { get; set; } = () => "";

    // Raised when a player goes online, by any door.
    public event Action<Session>? Opened;

    public ServerTerminals(TerminalNetwork network, Network session, Func<IEnumerable<Session>> sessions)
    {
        _network = network;
        _session = session;
        _sessions = sessions;
    }

    public void Use(Session session, Terminal terminal)
    {
        string key = session.ZoneId + "/" + terminal.Name;
        string? refusal = _access.Use(session.Record!.PlayerId, key, terminal.Enabled);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        _terminalsByKey[key] = terminal;
        terminal.UsedBy = session.Record.DisplayName;
        session.Body!.IsOnline = true;
        _network.SendOpened(session.PeerId, terminal.TypeId, terminal.Name);
        SendRoster(session);
        _network.SendStatus(session.PeerId, Status());
        Opened?.Invoke(session);
    }

    // The phone is a door of its own: one per player, so its key is the player's. The
    // caller has checked the phone can go online (equipped, with charge).
    public void UsePhone(Session session)
    {
        string? refusal = _access.Use(session.Record!.PlayerId, "phone/" + session.Record.PlayerId, true);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        _onPhone.Add(session.Record.PlayerId);
        session.Body!.IsOnline = true;
        session.Body.OnPhone = true;
        _network.SendOpened(session.PeerId, (int)TerminalType.Phone, "Phone");
        SendRoster(session);
        _network.SendStatus(session.PeerId, Status());
        Opened?.Invoke(session);
    }

    // A line on the status board, sent at once to everyone online.
    public void Post(string text)
    {
        _status.Post(Clock(), text);
        string[] lines = Status();

        foreach (Session session in _sessions())
        {
            if (session.Record != null && _access.IsOnline(session.Record.PlayerId))
            {
                _network.SendStatus(session.PeerId, lines);
            }
        }
    }

    private string[] Status()
    {
        return new List<string>(_status.Lines).ToArray();
    }

    public bool IsOnPhone(Session session)
    {
        return session.Record != null && _onPhone.Contains(session.Record.PlayerId);
    }

    // Going offline by choice, which closes the screen.
    public void Leave(Session session)
    {
        if (Release(session))
        {
            _network.SendClosed(session.PeerId);
        }
    }

    // A disconnect: the terminal is freed; there is no screen left to close.
    public void Disconnected(Session session)
    {
        Release(session);
    }

    public void Tick(double delta)
    {
        _sinceRoster += delta;

        if (_sinceRoster < RosterIntervalSeconds)
        {
            return;
        }

        _sinceRoster = 0;

        // Built once for everyone, and sent only when it changed: with everyone online
        // and a roster built per viewer, this was the whole server's worst frame (a
        // hundred rosters of a hundred, 13 ms, every 2 s).
        string[] names;
        string[] zones;
        int[] online;
        BuildRoster(out names, out zones, out online);
        string key = string.Join("/", names) + "|" + string.Join("/", zones) + "|" + string.Join(",", online);

        if (key == _rosterKey)
        {
            return;
        }

        _rosterKey = key;
        List<long> peers = new List<long>();

        foreach (Session session in _sessions())
        {
            if (session.Record != null && _access.IsOnline(session.Record.PlayerId))
            {
                peers.Add(session.PeerId);
            }
        }

        _network.SendRoster(peers, names, zones, online);
    }

    private bool Release(Session session)
    {
        if (session.Record == null)
        {
            return false;
        }

        string? key = _access.Leave(session.Record.PlayerId);

        if (key == null)
        {
            return false;
        }

        _onPhone.Remove(session.Record.PlayerId);

        Terminal? terminal;

        if (_terminalsByKey.TryGetValue(key, out terminal))
        {
            terminal.UsedBy = "";
            _terminalsByKey.Remove(key);
        }

        if (session.Body != null)
        {
            session.Body.IsOnline = false;
            session.Body.OnPhone = false;
        }

        return true;
    }

    private void SendRoster(Session viewer)
    {
        string[] names;
        string[] zones;
        int[] online;
        BuildRoster(out names, out zones, out online);
        _network.SendRoster(viewer.PeerId, names, zones, online);
    }

    private void BuildRoster(out string[] names, out string[] zones, out int[] online)
    {
        List<string> nameList = new List<string>();
        List<string> zoneList = new List<string>();
        List<int> onlineList = new List<int>();

        foreach (Session session in _sessions())
        {
            if (session.State == SessionState.InWorld && session.Record != null)
            {
                nameList.Add(session.Record.DisplayName);
                zoneList.Add(session.Record.Zone);
                onlineList.Add(_access.IsOnline(session.Record.PlayerId) ? 1 : 0);
            }
        }

        names = nameList.ToArray();
        zones = zoneList.ToArray();
        online = onlineList.ToArray();
    }
}
