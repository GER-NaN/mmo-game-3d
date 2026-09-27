namespace MmoGame3d.Rules.Town;

/// <summary>
/// How a townsperson walks their loop: at a walking pace, stopping now and then to stand
/// a while, then going on. Distances are metres along the loop, which wraps. The random
/// source is passed in, so a test can fix it. Placeholder numbers.
/// </summary>
public class Stroll
{
    public const float Pace = 1.4f;
    public const float MinWalk = 10f;
    public const float MaxWalk = 30f;
    public const double MinPause = 3;
    public const double MaxPause = 8;

    private readonly Random _random;
    private readonly float _loopLength;
    private float _untilPause;
    private double _pauseLeft;

    public Stroll(float loopLength, float startAt, Random random)
    {
        _loopLength = loopLength;
        _random = random;
        Along = startAt % loopLength;
        _untilPause = NextWalk();
    }

    // Metres along the loop, 0 up to its length.
    public float Along { get; private set; }

    public bool Walking
    {
        get { return _pauseLeft <= 0; }
    }

    // Someone talks to them: they stand for at least this long, then go on.
    public void Stop(double seconds)
    {
        _pauseLeft = Math.Max(_pauseLeft, seconds);
    }

    public void Advance(double seconds)
    {
        if (_pauseLeft > 0)
        {
            _pauseLeft -= seconds;
            return;
        }

        float step = Pace * (float)seconds;
        Along = (Along + step) % _loopLength;
        _untilPause -= step;

        if (_untilPause <= 0)
        {
            _untilPause = NextWalk();
            _pauseLeft = MinPause + (_random.NextDouble() * (MaxPause - MinPause));
        }
    }

    private float NextWalk()
    {
        return MinWalk + ((float)_random.NextDouble() * (MaxWalk - MinWalk));
    }
}
