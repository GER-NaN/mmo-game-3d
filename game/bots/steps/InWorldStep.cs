namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Ui;

/// <summary>
/// In the world, with nothing open: where most activities begin. It is also the bot's
/// keeper (bots.md R6). Left at the main menu (a lost connection) it presses Play; at the
/// character screen, Play on the first character; with screens left open by an activity
/// cut short, it closes each with its own key, as a player would. Clear twice in a row,
/// since a screen asked for just before (a phone key mashed) opens a moment later.
/// </summary>
public class InWorldStep : BotStep
{
    private const double LookInterval = 0.25;

    // The client may be on its way by itself (--autoconnect): a menu must stay up this
    // long before the bot presses anything on it.
    private const double MenuPatience = 3;

    // Between closing keys, so each screen has gone before the next key.
    private const double CloseGap = 0.5;

    private double _sinceLook = LookInterval;
    private double _menuFor;
    private double _sinceClose = CloseGap;
    private int _tries;
    private int _clearLooks;

    public InWorldStep()
        : base("in world", 60)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _sinceLook += delta;
        _sinceClose += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        double looked = _sinceLook;
        _sinceLook = 0;
        ClientView? view = body.View;

        if (view == null)
        {
            return BotStepState.Running;
        }

        if (body.Player == null || body.Zone == null)
        {
            BackIntoTheWorld(body, looked);
            return BotStepState.Running;
        }

        _menuFor = 0;
        List<string> toClose = new List<string>();

        foreach (string screen in view.OpenScreens)
        {
            if (BotScreens.CloseKey(screen) != null)
            {
                toClose.Add(screen);
            }
        }

        // A chat line left open (an activity walked away from mid-line) has the keys too.
        if (body.FocusedField() != null)
        {
            toClose.Add(BotScreens.TextField);
        }

        if (toClose.Count == 0)
        {
            _clearLooks++;

            if (_clearLooks < 2)
            {
                return BotStepState.Running;
            }

            body.Events.Write("in-world", body.Zone.ZoneId);
            return BotStepState.Done;
        }

        _clearLooks = 0;

        if (_sinceClose >= CloseGap)
        {
            _sinceClose = 0;
            _tries++;

            if (_tries > 10)
            {
                return Fail("cannot close " + string.Join(", ", toClose));
            }

            string key = BotScreens.CloseKey(toClose[0])!;
            body.Events.Write("closing", toClose[0] + " with " + key);
            body.Key(key, true);
            body.Key(key, false);
        }

        return BotStepState.Running;
    }

    // At the main menu or the character screen for a while: Play.
    private void BackIntoTheWorld(BotBody body, double looked)
    {
        MainMenu? menu = body.Find<MainMenu>();
        CharacterSelect? select = body.Find<CharacterSelect>();

        if (menu == null && select == null)
        {
            _menuFor = 0;
            return;
        }

        _menuFor += looked;

        if (_menuFor < MenuPatience)
        {
            return;
        }

        _menuFor = 0;
        Button? play = menu != null ? menu.GetNodeOrNull<Button>("%Play") : BotScreens.FirstPlay(select!);

        if (play != null && !play.Disabled)
        {
            body.Events.Write("keeper", "Play on the " + (menu != null ? "main menu" : "character screen"));
            body.Click(play);
        }
    }
}
