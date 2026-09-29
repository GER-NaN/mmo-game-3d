namespace MmoGame3d.Bots;

/// <summary>
/// Types a line into the text field that has the keys (chat, a name), then Enter, as a
/// player types it. Waits for a field to have the keys first.
/// </summary>
public class TypeStep : BotStep
{
    private readonly System.Func<BotBody, string> _text;

    public TypeStep(string text)
        : base("type \"" + text + "\"", DefaultTimeLimit)
    {
        _text = body => text;
    }

    // Text chosen as it is typed (a random name, from the bot's seed).
    public TypeStep(string what, System.Func<BotBody, string> text)
        : base("type " + what, DefaultTimeLimit)
    {
        _text = text;
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        if (body.FocusedField() == null)
        {
            return BotStepState.Running;
        }

        string text = _text(body);
        body.Events.Write("typed", text);
        body.Type(text);
        return BotStepState.Done;
    }
}
