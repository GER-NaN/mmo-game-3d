namespace MmoGame3d.Rules.Terminals;

// A door into the terminal world. These are different access points, not ranks:
// nothing reads one as better than another.
public enum TerminalType
{
    Public,
    Phone,
    Laptop,
    GamingRig,
    Supercomputer,
    DataCentre,
}

// Whether an app can be opened, and if not, why.
public enum AppState
{
    Open,

    // In the game, not reachable yet: shown with a locked notice, so a player sees what
    // exists before they can use it.
    Locked,
}

public class TerminalApp
{
    public TerminalApp(string id, string name, string lockedNotice)
    {
        Id = id;
        Name = name;
        LockedNotice = lockedNotice;
    }

    public string Id { get; }
    public string Name { get; }

    // Empty for an app that opens.
    public string LockedNotice { get; }

    public AppState State
    {
        get { return LockedNotice.Length == 0 ? AppState.Open : AppState.Locked; }
    }
}

/// <summary>
/// The terminal OS's apps, from world.md section 4: every feature of the game is an
/// app, and the locked ones still show, so a player sees the whole game from a
/// terminal. Which apps a door offers depends on the door: a phone has only simple
/// ones.
/// </summary>
public static class TerminalApps
{
    public const string Chat = "chat";
    public const string Online = "online";
    public const string StatusBoard = "status";
    public const string ExchangeRate = "exchange";
    public const string TodoList = "todo";
    public const string TownLog = "townlog";
    public const string CodeCracker = "crack";
    public const string Whois = "whois";

    // Old Town's own cameras: spotting drones, the first Defense Objective of the kind
    // world.md lists ("operate the CCTV camera to spot enemy drone activity").
    public const string TownCameras = "cameras";
    public const string Defense = "defense";

    private static readonly TerminalApp[] All =
    {
        new TerminalApp(Chat, "Chat", ""),
        new TerminalApp(Online, "Who's online", ""),
        new TerminalApp(Whois, "Whois", ""),
        new TerminalApp(TodoList, "Town repairs", ""),
        new TerminalApp(TownLog, "Town log", ""),
        new TerminalApp(CodeCracker, "Code cracker", ""),
        new TerminalApp(TownCameras, "Town cameras", ""),
        new TerminalApp(StatusBoard, "Status board", ""),
        new TerminalApp(ExchangeRate, "Exchange rate", ""),
        new TerminalApp(Defense, "Defense Objectives", ""),
        new TerminalApp("cctv", "Remote monitoring", "Locked. CCTV from other towns needs access you do not have yet."),
        new TerminalApp("fpv", "FPV drone surveillance", "Locked. You need a drone first."),
        new TerminalApp("market", "Market prices", "Locked. The market opens in a bigger town."),
        new TerminalApp("wallet", "Crypto wallet", "Locked. You have no wallet yet."),
        new TerminalApp("meeting", "Meeting room", "Locked. Meeting rooms open with teams."),
        new TerminalApp("transit", "Transit schedule", "Locked. There is no transit here yet."),
    };

    // What a phone keeps: the simple apps, and it still shows the locked ones.
    private static readonly HashSet<string> PhoneApps = new HashSet<string> { Chat, Online, Whois, TodoList, Defense, "wallet" };

    public static List<TerminalApp> For(TerminalType door)
    {
        List<TerminalApp> apps = new List<TerminalApp>();

        foreach (TerminalApp app in All)
        {
            if (door != TerminalType.Phone || PhoneApps.Contains(app.Id))
            {
                apps.Add(app);
            }
        }

        return apps;
    }
}
