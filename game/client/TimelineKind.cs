namespace MmoGame3d.Client;

/// <summary>
/// What a timeline entry records (docs/features/timeline.md).
/// </summary>
public enum TimelineKind
{
    ZoneEntered,
    ZoneExited,
    ScreenOpened,
    ScreenClosed,

    // F on a thing in reach.
    Interacted,

    // A line the player submitted; the server's copy of it comes back as ChatReceived.
    ChatSent,
    ChatReceived,

    Notice,
    MoneyChanged,
    ItemGained,
    ItemLost,
    PartyJoined,
    PartyLeft,
}
