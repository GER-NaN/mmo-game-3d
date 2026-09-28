namespace MmoGame3d.BotJudging;

public enum ClickMove
{
    // Clickable where it is.
    Click,

    // Just made: no size or place until the next layout.
    Wait,

    // In a list, outside what the list shows: turn the wheel over the list.
    WheelDown,
    WheelUp,

    // Nothing brings it into view: a finding, never a click.
    OffScreen,
}
