namespace MmoGame3d.Server;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Networking;
using MmoGame3d.Vendors;
using MmoGame3d.Terminals;
using MmoGame3d.Zones;

/// <summary>
/// A player asked to use something by name. The server finds it in the player's own
/// zone, checks the player is in reach, and hands it to whoever owns that kind of thing.
/// A name that is not there, or out of reach, is refused: closed by default.
/// </summary>
public class ServerInteractions
{
    // A little slack over the reach the client checks, for the body having moved on by
    // the time the request arrives.
    private const float ReachSlack = 0.75f;

    private readonly World _world;
    private readonly Network _session;
    private readonly ServerTerminals _terminals;
    private readonly ServerShops _shops;

    public ServerInteractions(World world, Network session, ServerTerminals terminals, ServerShops shops)
    {
        _world = world;
        _session = session;
        _terminals = terminals;
        _shops = shops;
    }

    public void Use(Session session, string interactableName)
    {
        if (session.Body == null || session.ZoneId == null || session.Body.IsOnline)
        {
            return;
        }

        Zone? zone = _world.GetZone(session.ZoneId);
        Interactable? thing = zone?.GetNodeOrNull<Interactable>(Interactable.ParentName + "/" + interactableName);

        if (thing == null)
        {
            return;
        }

        if (!thing.IsInReach(session.Body.GlobalPosition, ReachSlack))
        {
            _session.SendNotice(session.PeerId, "Too far away.");
            return;
        }

        switch (thing)
        {
            case Terminal terminal:
                _terminals.Use(session, terminal);
                break;
            case Vendor vendor:
                _shops.Open(session, vendor);
                break;
            default:
                GD.PrintErr("Nothing handles the interactable " + interactableName + " of type " + thing.GetType().Name);
                break;
        }
    }
}
