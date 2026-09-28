namespace MmoGame3d.Networking;

using System.Diagnostics;
using Godot;

public interface IRpcLog
{
    Activity? Received(string node, StringName method, long peer, object?[] args);

    void Sent(string node, StringName method, long peer, Variant[] args);

    void SentToMany(string node, StringName method, int peers, Variant[] args);
}
