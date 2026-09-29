namespace MmoGame3d.Bots;

/// <summary>
/// Waits for a notice from the server containing some words ("You fixed the"), counting
/// only notices that came after the step began. Empty words take any new notice.
/// </summary>
public class NoticeStep : BotStep
{
    private readonly string _words;
    private int _since = -1;

    public NoticeStep(string words, double timeLimit)
        : base(words.Length > 0 ? "until a notice saying \"" + words + "\"" : "until a notice", timeLimit)
    {
        _words = words;
    }

    public override void Start(BotBody body)
    {
        _since = body.View?.NoticeCount ?? 0;
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        foreach (string notice in body.NoticesSince(_since))
        {
            if (notice.Contains(_words))
            {
                body.Events.Write("notice", notice);
                return BotStepState.Done;
            }
        }

        return BotStepState.Running;
    }
}
