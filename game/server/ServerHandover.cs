namespace MmoGame3d.Server;

using System;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Items;
using MmoGame3d.Zones;

/// <summary>
/// Dropping things on the ground and giving them to another player. Both take things
/// away from the player, so both are intents (ServerIntents). A drop lands in front of
/// the player, past pickup reach, so they do not walk straight back into it; anyone can
/// pick it up after.
/// </summary>
public class ServerHandover
{
    // How far in front a drop lands: past the pickup sphere and the body's radius.
    private const float DropDistance = 1.8f;

    private readonly ServerIntents _intents;
    private readonly Network _session;
    private readonly GroundItems _ground;
    private readonly World _world;
    private readonly Func<long, Session?> _findSession;
    private readonly Action<Session> _bagChanged;

    public ServerHandover(ServerIntents intents, Network session, GroundItems ground, World world, Func<long, Session?> findSession, Action<Session> bagChanged)
    {
        _intents = intents;
        _session = session;
        _ground = ground;
        _world = world;
        _findSession = findSession;
        _bagChanged = bagChanged;
    }

    public void Drop(Session session, uint intentId, int typeId, int tierId, int quantity)
    {
        _intents.Run(session, intentId, () =>
        {
            Zone? zone = session.ZoneId == null ? null : _world.GetZone(session.ZoneId);

            if (session.Body == null || zone == null || !Known(typeId, tierId))
            {
                return "You cannot drop that here.";
            }

            ItemType type = (ItemType)typeId;
            ItemTier tier = (ItemTier)tierId;
            string? refusal = Handover.Take(session.Inventory!, type, tier, quantity);

            if (refusal != null)
            {
                return refusal;
            }

            // In front of the body, on the zone's floor height of the feet.
            Vector3 forward = -session.Body.Transform.Basis.Z;
            Vector3 spot = session.Body.Position + (new Vector3(forward.X, 0f, forward.Z).Normalized() * DropDistance);
            _ground.DropAt(zone, spot, type, tier, quantity);
            _bagChanged(session);
            _session.SendNotice(session.PeerId, "Dropped " + quantity + " " + ItemCatalog.Describe(type, tier) + ".");
            return "";
        });
    }

    public void Give(Session session, uint intentId, long targetPeer, int typeId, int tierId, int quantity, int dollars)
    {
        _intents.Run(session, intentId, () =>
        {
            Session? target = _findSession(targetPeer);

            if (target == null || target == session || target.State != SessionState.InWorld || target.Body == null || session.Body == null
                || target.ZoneId != session.ZoneId || target.Body.GlobalPosition.DistanceTo(session.Body.GlobalPosition) > Handover.GiveReach)
            {
                return "Stand next to someone to give them something.";
            }

            if (ServerSocial.Ignores(target, session))
            {
                return target.Record!.DisplayName + " cannot take that now.";
            }

            string what;

            if (dollars > 0)
            {
                int mine;
                int theirs;
                string? refusal = Handover.GiveDollars(session.Dollars, target.Dollars, dollars, out mine, out theirs);

                if (refusal != null)
                {
                    return refusal;
                }

                session.Dollars = mine;
                target.Dollars = theirs;
                what = "$" + dollars;
            }
            else
            {
                if (!Known(typeId, tierId))
                {
                    return "You cannot give that.";
                }

                string? refusal = Handover.Give(session.Inventory!, target.Inventory!, (ItemType)typeId, (ItemTier)tierId, quantity);

                if (refusal != null)
                {
                    return refusal;
                }

                what = quantity + " " + ItemCatalog.Describe((ItemType)typeId, (ItemTier)tierId);
            }

            _bagChanged(session);
            _bagChanged(target);
            _session.SendNotice(session.PeerId, "You gave " + target.Record!.DisplayName + " " + what + ".");
            _session.SendNotice(target.PeerId, session.Record!.DisplayName + " gave you " + what + ".");
            return "";
        });
    }

    private static bool Known(int typeId, int tierId)
    {
        return Enum.IsDefined(typeof(ItemType), typeId) && Enum.IsDefined(typeof(ItemTier), tierId);
    }
}
