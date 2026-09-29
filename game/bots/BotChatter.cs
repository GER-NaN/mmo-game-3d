namespace MmoGame3d.Bots;

/// <summary>
/// Now and then, a line in chat from the current activity's phrases, typed as a player
/// types it: the chat key, the line, Enter. Only in the world, while the step leaves the
/// keys free (waiting or walking), and when nothing else has them, so it never types into
/// a screen the plan is working. A walk pauses while the line is written, as it does for
/// a player.
/// </summary>
public class BotChatter
{
    // Placeholders: seconds before the first line, and between lines.
    private const double FirstMin = 15;
    private const double FirstMax = 30;
    private const double BetweenMin = 45;
    private const double BetweenMax = 90;

    private double _untilNext = -1;
    private string _line = "";
    private int _stage;

    public void Tick(BotBody body, BotActivityRun run, double delta)
    {
        string[] phrases = run.Activity.Phrases;

        if (_untilNext < 0)
        {
            _untilNext = Between(body, FirstMin, FirstMax);
        }

        switch (_stage)
        {
            case 0:
                _untilNext -= delta;

                if (_untilNext > 0 || phrases.Length == 0 || body.Player == null || !StepAllows(run.Step) || !body.KeysFree())
                {
                    return;
                }

                _line = phrases[body.Random.Next(phrases.Length)];
                body.Key("chat", true);
                _stage = 1;
                break;
            case 1:
                body.Key("chat", false);
                _stage = 2;
                break;
            case 2:
                // The chat line takes the keys once it opens; if it did not, try later.
                if (body.FocusedField() != null)
                {
                    body.Type(_line);
                    body.Events.Write("said", _line);
                }

                _stage = 0;
                _untilNext = Between(body, BetweenMin, BetweenMax);
                break;
        }
    }

    private static bool StepAllows(BotStep? step)
    {
        return step == null || step.Intent == BotIntent.Idle || step.Intent == BotIntent.Walking;
    }

    private static double Between(BotBody body, double min, double max)
    {
        return min + (body.Random.NextDouble() * (max - min));
    }
}
