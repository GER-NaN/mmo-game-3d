namespace MmoGame3d.Rules;

// Client and server must be the same build: Godot's RPCs and sync are matched by node
// path and method, so a mismatch fails in confusing ways. The login compares this and
// refuses early with a clear reason. Raise it whenever an RPC or synced property changes.
public static class GameVersion
{
    public const int Protocol = 17;
}
