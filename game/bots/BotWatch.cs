namespace MmoGame3d.Bots;

using System.Collections.Generic;

/// <summary>
/// The watchers a bot runs, and its findings: each written as a "finding" event with
/// where the bot was, and a picture when the watcher wants one. The same finding within a
/// minute is written once (bots.md R8).
/// </summary>
public class BotWatch
{
    private const double RepeatSeconds = 60;

    private readonly List<BotWatcher> _watchers = new List<BotWatcher>();
    private readonly Dictionary<string, double> _lastWritten = new Dictionary<string, double>();
    private double _clock;
    private int _pictures;

    public BotWatch()
    {
        _watchers.Add(new StuckWatcher());
        _watchers.Add(new OutOfBoundsWatcher());
        _watchers.Add(new FloatingWatcher());
        _watchers.Add(new ZoneChurnWatcher());
        _watchers.Add(new ClientErrorWatcher());
    }

    public int Findings { get; private set; }

    public void Tick(BotBody body, BotStep? step, double delta)
    {
        _clock += delta;

        foreach (BotWatcher watcher in _watchers)
        {
            string? detail = watcher.Look(body, step, delta);

            if (detail == null)
            {
                continue;
            }

            string key = watcher.Kind + "|" + detail;
            double last;

            if (_lastWritten.TryGetValue(key, out last) && _clock - last < RepeatSeconds)
            {
                continue;
            }

            _lastWritten[key] = _clock;
            Findings++;
            body.Events.Write("finding", watcher.Kind + ": " + detail + " at " + body.Where());

            if (watcher.WantsPicture)
            {
                _pictures++;
                body.SavePicture("finding-" + _pictures + "-" + watcher.Kind + ".png");
            }
        }
    }
}
