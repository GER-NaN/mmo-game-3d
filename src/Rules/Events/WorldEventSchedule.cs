namespace MmoGame3d.Rules.Events;
/// <summary>
/// When a definition's next event starts: one interval after the server starts, then
/// one interval after the last ended. One at a time; it does not wait for players.
/// </summary>
public sealed class WorldEventSchedule
{
    private readonly int _everySeconds;
    private double _nextAt;
    private bool _running;

    public WorldEventSchedule(int everySeconds, double now)
    {
        _everySeconds = everySeconds;
        _nextAt = now + everySeconds;
    }

    public double NextAt
    {
        get { return _nextAt; }
    }

    public bool Due(double now)
    {
        return !_running && now >= _nextAt;
    }

    public void Started()
    {
        _running = true;
    }

    public void Ended(double now)
    {
        _running = false;
        _nextAt = now + _everySeconds;
    }

    // Brought forward, for a dev scenario that wants one at once.
    public void StartNow(double now)
    {
        _nextAt = now;
    }
}
