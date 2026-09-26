namespace MmoGame3d.Rules.Social;

/// <summary>
/// One player's friends and ignored players, by player id, with the name each had when
/// added (names are not unique, so the id is what counts). Friends are one-way: adding
/// someone needs no answer from them, and only lets you see when they are online.
/// Ignoring someone drops them as a friend, and befriending drops the ignore, so nobody
/// is on both lists.
/// </summary>
public class Contacts
{
    public const int MaxFriends = 100;
    public const int MaxIgnored = 100;

    private readonly Dictionary<Guid, string> _friends = new Dictionary<Guid, string>();
    private readonly Dictionary<Guid, string> _ignored = new Dictionary<Guid, string>();

    public IReadOnlyDictionary<Guid, string> Friends
    {
        get { return _friends; }
    }

    public IReadOnlyDictionary<Guid, string> Ignored
    {
        get { return _ignored; }
    }

    public bool IsFriend(Guid playerId)
    {
        return _friends.ContainsKey(playerId);
    }

    public bool Ignores(Guid playerId)
    {
        return _ignored.ContainsKey(playerId);
    }

    // Loading from the store: no rules, it was checked when added.
    public void Load(Guid playerId, string name, bool ignored)
    {
        if (ignored)
        {
            _ignored[playerId] = name;
        }
        else
        {
            _friends[playerId] = name;
        }
    }

    // Null when added, or the reason not.
    public string? Befriend(Guid self, Guid playerId, string name)
    {
        if (playerId == self)
        {
            return "You cannot add yourself as a friend.";
        }

        if (_friends.ContainsKey(playerId))
        {
            return name + " is already your friend.";
        }

        if (_friends.Count >= MaxFriends)
        {
            return "Your friends list is full.";
        }

        _ignored.Remove(playerId);
        _friends[playerId] = name;
        return null;
    }

    public string? Ignore(Guid self, Guid playerId, string name)
    {
        if (playerId == self)
        {
            return "You cannot ignore yourself.";
        }

        if (_ignored.ContainsKey(playerId))
        {
            return name + " is already ignored.";
        }

        if (_ignored.Count >= MaxIgnored)
        {
            return "Your ignore list is full.";
        }

        _friends.Remove(playerId);
        _ignored[playerId] = name;
        return null;
    }

    // True when they were on either list.
    public bool Remove(Guid playerId)
    {
        bool wasFriend = _friends.Remove(playerId);
        bool wasIgnored = _ignored.Remove(playerId);
        return wasFriend || wasIgnored;
    }
}
