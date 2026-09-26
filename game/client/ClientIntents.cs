namespace MmoGame3d.Client;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// The client's half of intents: each gets an id, is sent, and is sent again with the
/// same id until the server answers. The server acts on an id once, so a resend never
/// buys twice. Ids start at a random point, so a quick reconnect does not reuse the ids
/// of the session before.
/// </summary>
public partial class ClientIntents : Node
{
    private const double ResendSeconds = 3;

    private readonly Dictionary<uint, Pending> _pending = new Dictionary<uint, Pending>();
    private uint _nextId = (uint)Random.Shared.Next();

    // (what it was for, "" when approved or the refusal).
    public event Action<string, string>? Answered;

    public int Waiting
    {
        get { return _pending.Count; }
    }

    // send is called now and on every resend, with the intent's id.
    public void Start(string label, Action<uint> send)
    {
        uint id = _nextId++;
        _pending[id] = new Pending(label, send);
        send(id);
    }

    public void Answer(uint id, string refusal)
    {
        Pending? pending;

        if (_pending.TryGetValue(id, out pending))
        {
            _pending.Remove(id);
            Answered?.Invoke(pending.Label, refusal);
        }
    }

    public override void _Process(double delta)
    {
        foreach (KeyValuePair<uint, Pending> entry in _pending)
        {
            entry.Value.SinceSent += delta;

            if (entry.Value.SinceSent >= ResendSeconds)
            {
                entry.Value.SinceSent = 0;
                GD.Print("Resending intent " + entry.Key + " (" + entry.Value.Label + ")");
                entry.Value.Send(entry.Key);
            }
        }
    }

    private class Pending
    {
        public Pending(string label, Action<uint> send)
        {
            Label = label;
            Send = send;
        }

        public string Label { get; }
        public Action<uint> Send { get; }
        public double SinceSent { get; set; }
    }
}
