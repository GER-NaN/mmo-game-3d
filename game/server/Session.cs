namespace MmoGame3d.Server;

using System.Collections.Generic;
using MmoGame3d.Data.Players;
using MmoGame3d.Players;
using MmoGame3d.Rules.Intents;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Maps;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;
using MmoGame3d.Vendors;
using MmoGame3d.Workbenches;

/// <summary>
/// One connected client, from connect to disconnect. The peer id is the session's
/// identity: ENet binds it to the connection, and Godot stamps it on every RPC as the
/// sender, so a client cannot claim another's. That is why there is no session token.
/// </summary>
public class Session
{
    public Session(long peerId)
    {
        PeerId = peerId;
    }

    public long PeerId { get; }

    public SessionState State { get; set; } = SessionState.Connected;

    // Set once the hello is answered: whose characters this client may play.
    public System.Guid? AccountId { get; set; }

    // Set once the character is loaded.
    public PlayerRecord? Record { get; set; }

    // What they carry, live. Set with the record.
    public Inventory? Inventory { get; set; }

    // Pocket change, live. Set with the record.
    public int Dollars { get; set; }

    // The answers given to this player's intents, so a resent one is not acted on twice.
    public IntentLedger Intents { get; } = new IntentLedger();

    // The shopkeeper last used; a buy is taken only while they are in reach.
    public Vendor? OpenVendor { get; set; }

    // Things with an identity (a phone, a battery), live. Set with the record.
    public List<ItemInstance> Instances { get; set; } = new List<ItemInstance>();

    // The college person last talked to; their requests are taken only while in reach.
    public College.CollegePerson? OpenCollegePerson { get; set; }

    // The workbench last used; work is taken only while it is in reach.
    public Workbench? OpenWorkbench { get; set; }

    // The potting table last used; a plant is taken only while it is in reach.
    public Gardening.PottingTable? OpenPottingTable { get; set; }

    // The recycler last used; things go in only while it is in reach.
    public Town.Recycler? OpenRecycler { get; set; }

    // True while a Mechanical Engineer works from their repair pack instead of a bench.
    public bool UsingRepairPack { get; set; }

    // Skills, career, time played, missions, live. Set with the record.
    public PlayerProgress Progress { get; set; } = new PlayerProgress();

    // The achievements earned, for granting each once.
    public HashSet<string> Achievements { get; set; } = new HashSet<string>();

    // Distance and jumps not yet worth a whole Agility experience.
    public AgilityCounter Agility { get; } = new AgilityCounter();

    // HP, kept here so it goes through doors with the player; the body shows it.
    public int Health { get; set; } = Rules.Players.Health.Max;
    public double SinceHurt { get; set; }

    // Whether the player was told this session that the next rank is ready.
    public bool RankReadyNoted { get; set; }

    // The Whois page's settings (Plan, what is shown), live. Set with the record.
    public WhoisSettings Page { get; set; } = new WhoisSettings();

    // Friends and ignores, live. Set with the record.
    public Contacts Contacts { get; set; } = new Contacts();

    // Where they have been, per zone with a map, live; and the zones changed since the
    // last save. Set with the record.
    public Dictionary<string, Discovery> Maps { get; } = new Dictionary<string, Discovery>();
    public HashSet<string> UnsavedMaps { get; } = new HashSet<string>();

    // Set once the client has its world loaded and the body is spawned. Null again while
    // the player goes through a door, until the next zone is loaded.
    public Player? Body { get; set; }

    // True from the first spawn on: the player has been in the world this session, so
    // there is something to save and a leave to announce.
    public bool HasEnteredWorld { get; set; }

    public string? ZoneId
    {
        get { return Record?.Zone; }
    }
}

public enum SessionState
{
    // Connected, no hello yet.
    Connected,

    // The account is known; the player is choosing or making a character.
    Choosing,

    // Login sent, the account lookup is running.
    LoggingIn,

    // Accepted; the client is loading its zone.
    Accepted,

    // The body is in the world.
    InWorld,
}
