namespace MmoGame3d.Rules.Skills;

/// <summary>
/// Agility counts distance and jumps: each full step of either is one experience. The
/// remainders live for the session only; losing part of a step at logout is not worth
/// storing.
/// </summary>
public class AgilityCounter
{
    private float _metres;
    private int _jumps;

    // Experience earned by this stretch of walking.
    public long Walked(float metres)
    {
        _metres += metres;
        long xp = (long)(_metres / SkillAwards.AgilityMetresPerXp);
        _metres -= xp * SkillAwards.AgilityMetresPerXp;
        return xp;
    }

    public long Jumped()
    {
        _jumps++;

        if (_jumps < SkillAwards.AgilityJumpsPerXp)
        {
            return 0;
        }

        _jumps = 0;
        return 1;
    }
}
