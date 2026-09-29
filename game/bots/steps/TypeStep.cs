namespace MmoGame3d.Bots;

/// <summary>
/// Types a line into the text field that has the keys (chat, a name), then Enter, as a
/// player types it. Waits for a field to have the keys first.
/// </summary>
public class TypeStep : BotStep
{
    private readonly string _text;

    public TypeStep(string text)
        : base("type \"" + text + "\"", DefaultTimeLimit)
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

        body.Type(_text);
        return BotStepState.Done;
    }
}
