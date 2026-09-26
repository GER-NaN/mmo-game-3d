namespace MmoGame3d.Rules.Town;

using System;

/// <summary>
/// The second town job: the AI puts rootkits in the robo taxis, and a taxi with one is
/// not used until it is cleaned (world.md, What players change). A player takes the job
/// in the terminal and cleans it by cracking a code at a public terminal: the rootkit's.
/// The taxi stand shows whether the taxis are clean. Like the street lights, a cleaning
/// holds for a while, then the AI gets back in.
/// </summary>
public class TaxiRootkit
{
    public const string JobId = "taxi-rootkit";
    public const string JobTitle = "Rootkit in the robo taxis";
    public const string JobText = "Crack the rootkit's code with the code cracker at a public terminal. The taxis run again once it is out.";

    // Placeholder: how long a cleaning holds before the AI slips a rootkit back in.
    public static readonly TimeSpan HoldsFor = TimeSpan.FromHours(3);

    public bool Clean { get; private set; }
    public DateTime? CleanedAtUtc { get; private set; }
    public string CleanedBy { get; private set; } = "";

    public static TaxiRootkit Infected()
    {
        return new TaxiRootkit();
    }

    public static TaxiRootkit Restore(bool clean, DateTime? cleanedAtUtc, string cleanedBy)
    {
        return new TaxiRootkit { Clean = clean, CleanedAtUtc = cleanedAtUtc, CleanedBy = cleanedBy };
    }

    // Null when the code just cracked cleans the taxis, else why not.
    public string? CannotClean(bool hasTakenJob)
    {
        if (Clean)
        {
            return "The robo taxis are clean.";
        }

        if (!hasTakenJob)
        {
            return "Take the job in a terminal first: Town repairs.";
        }

        return null;
    }

    public void CleanOut(string by, DateTime nowUtc)
    {
        Clean = true;
        CleanedAtUtc = nowUtc;
        CleanedBy = by;
    }

    // True when the AI just got back in; the caller logs it.
    public bool InfectIfDue(DateTime nowUtc)
    {
        if (Clean && CleanedAtUtc != null && nowUtc - CleanedAtUtc.Value >= HoldsFor)
        {
            Clean = false;
            return true;
        }

        return false;
    }
}
