namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.BotJudging;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Players;
using MmoGame3d.Rules.Social;

/// <summary>
/// An emote typed in chat (/wave, /cheer, /sit, /pushups): an aside, now and then while the
/// bot is free and sometimes in the middle of something else.
/// </summary>
public sealed class EmoteActivity : StepsActivity
{
    private static readonly string[] Emotes = { "wave", "cheer", "sit", "pushups" };

    private string _emote = "wave";

    public EmoteActivity()
        : base("emote", 1)
    {
    }

    public override BotTiming Timing
    {
        get { return BotTiming.Aside; }
    }

    public override double AsideEvery
    {
        get { return 60; }
    }

    public override bool AllowsAsides
    {
        get { return false; }
    }

    public override double UsualSeconds
    {
        get { return 3; }
    }

    public override bool CanStart(BotBody body)
    {
        return body.Me != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        _emote = Emotes[body.Random.Next(Emotes.Length)];
        return new BotPlan().Step(ChatUi.Say("/" + _emote)).Pause(2).Steps;
    }

    public override BotActivityJudge? NewJudge()
    {
        return new EmoteJudge(this);
    }

    // The one picked for this run, for the judge.
    public string Emote
    {
        get { return _emote; }
    }
}

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

    public EmoteJudge(EmoteActivity activity)
    {
        _activity = activity;
    }

    public override void Before(BotBody body)
    {
        _online = body.IsOnline;
        _clear = !_online && body.OpenPanels().Count == 0;
        _seen = false;
    }

    public override string? Watch(BotBody body, double delta)
    {
        Player? me = body.Me;
        _seen = _seen || (me != null && me.GestureId == _activity.Emote && Gestures.Find(_activity.Emote) != null);
        return null;
    }

    public override string? After(BotBody body, BotEnd end)
    {
        return end == BotEnd.Finished ? EmoteCheck.Judge(_activity.Emote, _clear, _online, _seen) : null;
    }
}
