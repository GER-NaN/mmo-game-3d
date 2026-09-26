namespace MmoGame3d.Server;

using System;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Social;
using MmoGame3d.Town;

/// <summary>
/// The recycler: things in, pocket change out. Taking things away and paying for them,
/// so each use is an intent (ServerIntents), and only while the machine is in reach.
/// </summary>
public class ServerRecycling
{
    private const float ReachSlack = 1.5f;

    private readonly ItemNetwork _network;
    private readonly Network _session;
    private readonly ServerIntents _intents;
    private readonly Action<Session> _bagChanged;

    public ServerRecycling(ItemNetwork network, Network session, ServerIntents intents, Action<Session> bagChanged)
    {
        _network = network;
        _session = session;
        _intents = intents;
        _bagChanged = bagChanged;
    }

    public void Open(Session session, Recycler recycler)
    {
        session.OpenRecycler = recycler;
        _network.SendRecyclerOpened(session.PeerId);
    }

    public void RecycleOne(Session session, uint intentId, int typeId, int tierId)
    {
        _intents.Run(session, intentId, () =>
        {
            if (!AtRecycler(session))
            {
                return "You need to be at the recycler.";
            }

            if (!Enum.IsDefined(typeof(ItemType), typeId) || !Enum.IsDefined(typeof(ItemTier), tierId))
            {
                return "The recycler does not take that.";
            }

            int dollars;
            string? refusal = Recycling.RecycleOne(session.Inventory!, (ItemType)typeId, (ItemTier)tierId, out dollars);
            return Paid(session, refusal, dollars, ItemCatalog.Describe((ItemType)typeId, (ItemTier)tierId));
        });
    }

    public void RecycleThing(Session session, uint intentId, string instanceIdText)
    {
        _intents.Run(session, intentId, () =>
        {
            Guid id;

            if (!AtRecycler(session) || !Guid.TryParse(instanceIdText, out id))
            {
                return "You need to be at the recycler.";
            }

            Belongings mine = new Belongings(session.Inventory!, session.Instances);
            ItemInstance? thing = mine.Find(id);
            string what = thing == null ? "" : ItemCatalog.Describe(thing.Type, thing.Tier);
            int dollars;
            string? refusal = Recycling.RecycleThing(mine, id, out dollars);
            return Paid(session, refusal, dollars, what);
        });
    }

    private string Paid(Session session, string? refusal, int dollars, string what)
    {
        if (refusal != null)
        {
            return refusal;
        }

        session.Dollars += dollars;
        session.Body?.Show(Gestures.Work);
        _bagChanged(session);
        _session.SendNotice(session.PeerId, "Recycled " + what + " for $" + dollars + ".");
        return "";
    }

    private bool AtRecycler(Session session)
    {
        Recycler? recycler = session.OpenRecycler;
        return recycler != null && Godot.GodotObject.IsInstanceValid(recycler) && session.Body != null
            && recycler.IsInReach(session.Body.GlobalPosition, ReachSlack);
    }
}
