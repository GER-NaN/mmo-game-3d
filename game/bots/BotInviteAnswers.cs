namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Ui;

/// <summary>
/// Answers a party invite when one shows, after a moment's thought, as a player does:
/// joins or says no, at random (the persona's join chance), whatever the plan is doing.
/// Either way the prompt goes.
/// </summary>
public class BotInviteAnswers
{
    // Placeholders.
    private const double ThinkMin = 1;
    private const double ThinkMax = 4;

    private InvitePrompt? _prompt;
    private double _thinking;
    private bool _answered;

    public void Tick(BotBody body, double delta, double joinChance)
    {
        InvitePrompt? prompt = body.Find<InvitePrompt>();

        if (prompt == null)
        {
            _prompt = null;
            return;
        }

        if (prompt != _prompt)
        {
            _prompt = prompt;
            _answered = false;
            _thinking = ThinkMin + (body.Random.NextDouble() * (ThinkMax - ThinkMin));
            return;
        }

        _thinking -= delta;

        if (_answered || _thinking > 0)
        {
            return;
        }

        bool join = body.Random.NextDouble() < joinChance;
        Button? button = prompt.GetNodeOrNull<Button>(join ? "%Join" : "%No");

        if (button != null && button.IsVisibleInTree())
        {
            body.Click(button);
            body.Events.Write("invite", join ? "joined" : "said no");
        }

        _answered = true;
    }
}
