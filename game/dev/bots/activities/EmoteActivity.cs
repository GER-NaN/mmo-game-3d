namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

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
