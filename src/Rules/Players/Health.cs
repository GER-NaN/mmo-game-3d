namespace MmoGame3d.Rules.Players;

/// <summary>
/// HP (world.md 6 and 8): 100 to 0, flat with progression. Below 50 you move slower; at
/// 0 you faint and are carried back to town. It comes back slowly when nothing has hurt
/// you for a while. The rates are placeholders.
/// </summary>
public static class Health
{
    public const int Max = 100;
    public const int SlowBelow = 50;
    public const float SlowFactor = 0.7f;

    // Regeneration starts this long after the last hurt, at this rate.
    public const double RegenAfterSeconds = 6;
    public const int RegenPerSecond = 2;

    public static int Hurt(int current, int amount)
    {
        return Math.Max(0, current - Math.Max(0, amount));
    }

    public static int Heal(int current, int amount)
    {
        return Math.Min(Max, current + Math.Max(0, amount));
    }

    public static bool IsSlowed(int current)
    {
        return current < SlowBelow;
    }

    public static bool HasFainted(int current)
    {
        return current <= 0;
    }
}
