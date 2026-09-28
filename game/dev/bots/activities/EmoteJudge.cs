namespace MmoGame3d.Dev.Activities;

using MmoGame3d.BotJudging;
using MmoGame3d.Players;
using MmoGame3d.Rules.Social;

/// <summary>
/// An emote as its player sees it: with nothing in the way (not online, no screen open),
/// the body does it within the two seconds after; online, where the server holds the body
/// still, it does not. With a screen open the chat key may not reach the chat line, so the
/// outcome is only logged.
/// </summary>
public sealed class EmoteJudge : BotActivityJudge
{
    private readonly EmoteActivity _activity;
    private bool _clear;
    private bool _online;
    private bool _seen;
    private string _zone = "";

    public EmoteJudge(EmoteActivity activity)
    {
        _activity = activity;
    }

    public override void Before(BotBody body)
    {
        _online = body.IsOnline;
        _clear = !_online && body.OpenPanels().Count == 0;
        _seen = false;
        _zone = body.ZoneId;
    }

    public override string? Watch(BotBody body, double delta)
    {
        Player? me = body.Me;
        _seen = _seen || (me != null && me.GestureId == _activity.Emote && Gestures.Find(_activity.Emote) != null);
        return null;
    }

    public override string? After(BotBody body, BotEnd end)
    {
        return end == BotEnd.Finished ? EmoteCheck.Judge(_activity.Emote, _clear, _online, _seen, body.ZoneId != _zone) : null;
    }
}
