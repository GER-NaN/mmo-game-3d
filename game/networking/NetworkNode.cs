namespace MmoGame3d.Networking;

using System.Diagnostics;
using Godot;

/// <summary>
/// What the RPC nodes share: every RPC the server receives and every RPC it sends goes
/// through here, so it can be logged. The log is set on the server only; on a client
/// it is null and nothing is logged.
/// </summary>
public partial class NetworkNode : Node
{
    private string? _name;

    public IRpcLog? Log { get; set; }

    // A span for handling one received RPC: whatever the handler does, the database work
    // it queues included, is traced under it.
    protected Activity? Received(StringName method, long peer, params object?[] args)
    {
        return Log?.Received(NodeName(), method, peer, args);
    }

    protected void SendTo(long peer, StringName method, params Variant[] args)
    {
        Log?.Sent(NodeName(), method, peer, args);
        RpcId(peer, method, args);
    }

    private string NodeName()
    {
        if (_name == null)
        {
            _name = Name.ToString();
        }

        return _name;
    }
}

public interface IRpcLog
{
    Activity? Received(string node, StringName method, long peer, object?[] args);

    void Sent(string node, StringName method, long peer, Variant[] args);
}
