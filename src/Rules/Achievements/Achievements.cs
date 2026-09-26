namespace MmoGame3d.Rules.Achievements;

using System.Collections.Generic;

/// <summary>
/// Achievements (world.md, How it looks: "explore all of the map, play every
/// objective"). Each is earned once, for good, and shown in the skills panel. The first
/// set follows what the game has so far; the list is expected to grow.
/// </summary>
public static class Achievements
{
    public const string ExploreOldTown = "explore-old-town";
    public const string ExploreOutskirts = "explore-outskirts";
    public const string CrackACode = "crack-a-code";
    public const string StreetLights = "street-lights";
    public const string DownADrone = "down-a-drone";
    public const string HousePlant = "house-plant";
    public const string SubwayTag = "subway-tag";
    public const string RoboTaxi = "robo-taxi";
    public const string Career = "career";
    public const string Friend = "friend";

    // A map counts as explored at this share of its cells: a few may sit where no one
    // can walk, behind buildings at the edge.
    public const int ExploredPercent = 95;

    public static readonly IReadOnlyList<Achievement> All = new List<Achievement>
    {
        new Achievement(ExploreOldTown, "Old Town, all of it", "Explore the whole map of Old Town."),
        new Achievement(ExploreOutskirts, "Off the path", "Explore the whole map of the outskirts."),
        new Achievement(CrackACode, "Code cracker", "Crack a code at a public terminal."),
        new Achievement(StreetLights, "Lights on", "Repair the street lights on Main Street."),
        new Achievement(DownADrone, "Grounded", "Bring down a drone with an EMP."),
        new Achievement(HousePlant, "Green thumb", "Make a house plant in the greenhouse."),
        new Achievement(SubwayTag, "I was here", "Spray your name on the subway wall."),
        new Achievement(RoboTaxi, "Passenger", "Ride a robo taxi."),
        new Achievement(Career, "Enrolled", "Start a career at the college."),
        new Achievement(Friend, "Friendly", "Add a friend."),
    };

    public static Achievement? Find(string id)
    {
        foreach (Achievement achievement in All)
        {
            if (achievement.Id == id)
            {
                return achievement;
            }
        }

        return null;
    }

    public static bool IsExplored(int discovered, int total)
    {
        return total > 0 && discovered * 100 >= total * ExploredPercent;
    }
}

public class Achievement
{
    public Achievement(string id, string title, string text)
    {
        Id = id;
        Title = title;
        Text = text;
    }

    public string Id { get; }
    public string Title { get; }
    public string Text { get; }
}
