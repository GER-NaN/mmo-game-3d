namespace MmoGame3d.Client;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Items;

/// <summary>
/// The client's record of what happened, in order (docs/features/timeline.md). ClientView
/// is what is true now; this is how it came to be. For this session only, the newest
/// entries, with a running count: a reader notes the count and later asks for what came
/// after it. It keeps the zone the player is in, so the other entries say where.
/// </summary>
public class Timeline
{
    private const int Kept = 1000;

    private readonly List<TimelineEntry> _entries = new List<TimelineEntry>();
    private string _zoneId = "";

    // Every entry ever added, including those no longer kept.
    public int Count { get; private set; }

    private static double Now
    {
        get { return Time.GetTicksMsec() / 1000.0; }
    }

    public void AddZone(TimelineKind kind, string zoneId)
    {
        Add(TimelineEntry.Zone(Now, kind, zoneId));
        _zoneId = kind == TimelineKind.ZoneEntered ? zoneId : "";
    }

    public void AddScreen(TimelineKind kind, Type screen)
    {
        Add(TimelineEntry.Screen(Now, kind, _zoneId, screen));
    }

    public void AddInteraction(Type thing, string which)
    {
        Add(TimelineEntry.Interaction(Now, _zoneId, thing, which));
    }

    public void AddChat(TimelineKind kind, ChatKind chat, string who, string text)
    {
        Add(TimelineEntry.ChatLine(Now, kind, _zoneId, chat, who, text));
    }

    public void AddNotice(string text)
    {
        Add(TimelineEntry.Notice(Now, _zoneId, text));
    }

    public void AddMoney(long change)
    {
        Add(TimelineEntry.Money(Now, _zoneId, change));
    }

    public void AddItems(TimelineKind kind, ItemType item, ItemTier tier, long quantity)
    {
        Add(TimelineEntry.Items(Now, kind, _zoneId, item, tier, quantity));
    }

    public void AddParty(TimelineKind kind)
    {
        Add(TimelineEntry.Party(Now, kind, _zoneId));
    }

    // The entries added after the given count, oldest first. Only the newest are kept, so
    // a very old count gets what is left.
    public List<TimelineEntry> Since(int count)
    {
        List<TimelineEntry> since = new List<TimelineEntry>();
        int first = Count - _entries.Count;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (first + i >= count)
            {
                since.Add(_entries[i]);
            }
        }

        return since;
    }

    private void Add(TimelineEntry entry)
    {
        _entries.Add(entry);
        Count++;

        if (_entries.Count > Kept)
        {
            _entries.RemoveAt(0);
        }
    }
}
