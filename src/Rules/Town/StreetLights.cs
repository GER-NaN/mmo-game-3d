namespace MmoGame3d.Rules.Town;

using MmoGame3d.Rules.Items;

/// <summary>
/// The first repair job, the spine of the first playable: the street lights on Main
/// Street are out; a player takes the job in the terminal, repairs the junction box in
/// town with a part, and the lamps work for everyone. The work is both a terminal task
/// and a walk (a choice made while building; world.md left it open).
///
/// Placeholder for the AI's side: a repair holds for a while, then the lights go dark
/// again and the job comes back.
/// </summary>
public class StreetLights
{
    public const string JobId = "streetlights";
    public const string JobTitle = "Street lights out on Main Street";
    public const string JobText = "Repair the junction box on Main Street. You need one RAM stick as the part.";
    public const ItemType Part = ItemType.RamStick;

    // How long a repair holds before the AI takes the lights out again.
    public static readonly TimeSpan HoldsFor = TimeSpan.FromHours(4);

    public bool Working { get; private set; }
    public DateTime? RepairedAtUtc { get; private set; }
    public string RepairedBy { get; private set; } = "";

    public static StreetLights Broken()
    {
        return new StreetLights();
    }

    public static StreetLights Restore(bool working, DateTime? repairedAtUtc, string repairedBy)
    {
        return new StreetLights { Working = working, RepairedAtUtc = repairedAtUtc, RepairedBy = repairedBy };
    }

    // Null when the repair may go ahead, else why not, worded for the player.
    public string? CannotRepair(bool hasTakenJob, Inventory bag)
    {
        if (Working)
        {
            return "The lights are working. Nothing to repair.";
        }

        if (!hasTakenJob)
        {
            return "Take the job in a terminal first: Town repairs.";
        }

        if (!HasPart(bag))
        {
            return "You need a RAM stick as the part.";
        }

        return null;
    }

    // Takes one part, of the lowest tier the player has, and fixes the lights.
    public void Repair(Inventory bag, string by, DateTime nowUtc)
    {
        foreach (ItemTier tier in Enum.GetValues<ItemTier>())
        {
            if (bag.TryRemove(Part, tier, 1))
            {
                break;
            }
        }

        Working = true;
        RepairedAtUtc = nowUtc;
        RepairedBy = by;
    }

    // True when the lights just went dark again; the caller logs it.
    public bool BreakIfDue(DateTime nowUtc)
    {
        if (Working && RepairedAtUtc != null && nowUtc - RepairedAtUtc.Value >= HoldsFor)
        {
            Working = false;
            return true;
        }

        return false;
    }

    private static bool HasPart(Inventory bag)
    {
        foreach (ItemTier tier in Enum.GetValues<ItemTier>())
        {
            if (bag.Count(Part, tier) > 0)
            {
                return true;
            }
        }

        return false;
    }
}
