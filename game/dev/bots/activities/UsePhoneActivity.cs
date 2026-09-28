namespace MmoGame3d.Dev.Activities;

using System.Collections.Generic;
using MmoGame3d.Dev.Screens;

/// <summary>The phone out, a few apps, offline: wherever the bot is.</summary>
public sealed class UsePhoneActivity : StepsActivity
{
    public UsePhoneActivity()
        : base("use the phone", 3)
    {
    }

    public override bool CanStart(BotBody body)
    {
        return body.Me != null;
    }

    protected override List<BotStep> Plan(BotBody body)
    {
        return new List<BotStep> { TerminalUi.PhoneOut(), TerminalUi.Browse() };
    }
}
