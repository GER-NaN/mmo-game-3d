namespace MmoGame3d.Rules.Skills;

/// <summary>
/// The player level: time played, skill experience, career progress and missions, added
/// up (player-skills.md F5). The formula is a placeholder until the numbers session.
/// </summary>
public static class PlayerLevel
{
    public const long PointsPerMission = 100;
    public const double PointsPerLevelStep = 100;

    public static int For(long minutesPlayed, long skillXp, long careerXp, long missions)
    {
        long points = minutesPlayed + skillXp + careerXp + (missions * PointsPerMission);
        return 1 + (int)Math.Floor(Math.Sqrt(Math.Max(0, points) / PointsPerLevelStep));
    }
}
