namespace MmoGame3d.Client;

using System;
using MmoGame3d.Rules.Chat;
using MmoGame3d.Rules.Items;

/// <summary>
/// One thing that happened to or by this client's player: when, where, what kind, and
/// what it was about. Things are typed (a class, an enum); only values are raw: an amount,
/// a line of text, a player's name (docs/features/timeline.md). Each kind fills only its
/// own fields; the rest keep their empty value.
/// </summary>
public class TimelineEntry
{
    private TimelineEntry(double time, TimelineKind kind, string zoneId)
    {
        Time = time;
        Kind = kind;
        ZoneId = zoneId;
    }

    // Seconds since the client started.
    public double Time { get; }

    public TimelineKind Kind { get; }

    // Where: the zone entered or left, else the zone it happened in; an id from ZoneIds
    // ("taxi-3" for a ride), empty outside the world.
    public string ZoneId { get; }

    // The screen opened or closed, or the thing used, by its class.
    public Type? Thing { get; private set; }

    // Which one: the thing's node name, when a zone has several of a class; for a chat
    // line, the other side (who said it, or to whom).
    public string Which { get; private set; } = "";

    public ChatKind? Chat { get; private set; }

    public ItemType? Item { get; private set; }

    public ItemTier? Tier { get; private set; }

    // Money changed by, or how many items came or went.
    public long Amount { get; private set; }

    // A chat line or a notice.
    public string Text { get; private set; } = "";

    public static TimelineEntry Zone(double time, TimelineKind kind, string zoneId)
    {
        return new TimelineEntry(time, kind, zoneId);
    }

    public static TimelineEntry Screen(double time, TimelineKind kind, string zoneId, Type screen)
    {
        return new TimelineEntry(time, kind, zoneId) { Thing = screen };
    }

    public static TimelineEntry Interaction(double time, string zoneId, Type thing, string which)
    {
        return new TimelineEntry(time, TimelineKind.Interacted, zoneId) { Thing = thing, Which = which };
    }

    public static TimelineEntry ChatLine(double time, TimelineKind kind, string zoneId, ChatKind chat, string who, string text)
    {
        return new TimelineEntry(time, kind, zoneId) { Chat = chat, Which = who, Text = text };
    }

    public static TimelineEntry Notice(double time, string zoneId, string text)
    {
        return new TimelineEntry(time, TimelineKind.Notice, zoneId) { Text = text };
    }

    public static TimelineEntry Money(double time, string zoneId, long change)
    {
        return new TimelineEntry(time, TimelineKind.MoneyChanged, zoneId) { Amount = change };
    }

    public static TimelineEntry Items(double time, TimelineKind kind, string zoneId, ItemType item, ItemTier tier, long quantity)
    {
        return new TimelineEntry(time, kind, zoneId) { Item = item, Tier = tier, Amount = quantity };
    }

    public static TimelineEntry Party(double time, TimelineKind kind, string zoneId)
    {
        return new TimelineEntry(time, kind, zoneId);
    }
}
