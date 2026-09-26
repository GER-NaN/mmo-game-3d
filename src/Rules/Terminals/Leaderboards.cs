namespace MmoGame3d.Rules.Terminals;

using System;

/// <summary>
/// The objectives' leaderboards: the top ten and your personal best (world.md, Defense
/// Objectives). Each objective says what its score is and whether lower is better.
/// </summary>
public static class Leaderboards
{
    public const int Shown = 10;

    // The code cracker's score is the guesses used: fewer is better, then faster.
    public const string CodeCracker = "code-cracker";

    // Agent Defense's score is points: more is better, then faster (all runs are a minute).
    public const string AgentDefense = "agent-defense";

    public static bool LowerIsBetter(string objective)
    {
        return objective == CodeCracker;
    }

    // "3 guesses, 0:42".
    public static string Result(string objective, int score, double seconds)
    {
        if (objective != CodeCracker)
        {
            return score + " points";
        }

        TimeSpan time = TimeSpan.FromSeconds(Math.Round(seconds));
        return score + (score == 1 ? " guess" : " guesses") + ", " + (int)time.TotalMinutes + ":" + time.Seconds.ToString("00");
    }
}
