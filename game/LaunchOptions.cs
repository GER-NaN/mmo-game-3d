namespace MmoGame3d;

using MmoGame3d.Data;
using MmoGame3d.Rules.Time;

/// <summary>
/// What this run of the game is, from the arguments after "--" on the command line.
///
///   --server            run as the headless server
///   --port 7070         the server's port (both sides)
///   --db "Host=..."     the server's database
///   --max-players 300   how many clients the server accepts
///   --time-zone id      the server's time zone (default America/New_York)
///   --time-offset 6     dev: shift the world's hour, so noon can be seen at midnight
///   --profile name      which player this client is (default "default"; "fresh" is a
///                       new player every launch)
///   --name Gerald       the display name for a new player
///   --address 1.2.3.4   the server to connect to
///   --autoconnect       skip the main menu and connect at once
///   --bot               the client plays by itself (implies --autoconnect)
///   --report-every 2    print what the client sees every 2 seconds
///   --screenshot x.png  save the window to a PNG a few seconds in, then quit
///   --overview          with --screenshot: look down on the whole zone
///   --screenshot-after 4  seconds in the world before the screenshot (default 4)
///   --load-test 50      run 50 bot clients in this one process (profiles load-0...)
///   --load-first 0      the first load bot's number, so processes do not share bots
///   --stats-every 10    the server prints players, frame times and traffic this often
///   --walk-test         the player walks circles and the client prints how smoothly it
///                       draws the walk (needs a window)
///   --watch-test        the same, standing still and measuring another player's walk
/// </summary>
public class LaunchOptions
{
    public const int DefaultPort = 7070;

    public bool IsServer { get; private set; }
    public int Port { get; private set; } = DefaultPort;
    public string DatabaseConnection { get; private set; } = Database.DefaultConnectionString;
    public int MaxPlayers { get; private set; } = 300;
    public string TimeZone { get; private set; } = WorldClock.DefaultTimeZone;
    public double TimeOffsetHours { get; private set; }
    public string Profile { get; private set; } = "default";
    public string? DisplayName { get; private set; }
    public string? Address { get; private set; }
    public bool AutoConnect { get; private set; }
    public bool Bot { get; private set; }
    public double ReportEverySeconds { get; private set; }
    public string? ScreenshotPath { get; private set; }
    public bool Overview { get; private set; }
    public double ScreenshotAfterSeconds { get; private set; } = 4;
    public int LoadTestBots { get; private set; }
    public int LoadFirst { get; private set; }
    public bool LoadBot { get; private set; }
    public double StatsEverySeconds { get; private set; }
    public bool WalkTest { get; private set; }
    public bool WatchTest { get; private set; }

    // One load-test bot: its own player, connecting at once, walking by itself.
    public LaunchOptions ForLoadBot(int number)
    {
        return new LaunchOptions
        {
            Port = Port,
            Address = Address,
            Profile = "load-" + number,
            DisplayName = "Load" + number,
            AutoConnect = true,
            LoadBot = true,
        };
    }

    public static LaunchOptions Parse(string[] args)
    {
        LaunchOptions options = new LaunchOptions();

        for (int i = 0; i < args.Length; i++)
        {
            string next = i + 1 < args.Length ? args[i + 1] : "";

            switch (args[i])
            {
                case "--server":
                    options.IsServer = true;
                    break;
                case "--port":
                    options.Port = int.Parse(next);
                    i++;
                    break;
                case "--db":
                    options.DatabaseConnection = next;
                    i++;
                    break;
                case "--max-players":
                    options.MaxPlayers = int.Parse(next);
                    i++;
                    break;
                case "--time-zone":
                    options.TimeZone = next;
                    i++;
                    break;
                case "--time-offset":
                    options.TimeOffsetHours = double.Parse(next, System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                    break;
                case "--profile":
                    options.Profile = next;
                    i++;
                    break;
                case "--name":
                    options.DisplayName = next;
                    i++;
                    break;
                case "--address":
                    options.Address = next;
                    i++;
                    break;
                case "--autoconnect":
                    options.AutoConnect = true;
                    break;
                case "--bot":
                    options.Bot = true;
                    options.AutoConnect = true;
                    break;
                case "--screenshot":
                    options.ScreenshotPath = next;
                    options.AutoConnect = true;
                    i++;
                    break;
                case "--screenshot-after":
                    options.ScreenshotAfterSeconds = double.Parse(next, System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                    break;
                case "--load-test":
                    options.LoadTestBots = int.Parse(next);
                    i++;
                    break;
                case "--load-first":
                    options.LoadFirst = int.Parse(next);
                    i++;
                    break;
                case "--stats-every":
                    options.StatsEverySeconds = double.Parse(next, System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                    break;
                case "--watch-test":
                    options.WatchTest = true;
                    options.WalkTest = true;
                    options.AutoConnect = true;
                    break;
                case "--walk-test":
                    options.WalkTest = true;
                    options.AutoConnect = true;
                    break;
                case "--overview":
                    options.Overview = true;
                    break;
                case "--report-every":
                    options.ReportEverySeconds = double.Parse(next, System.Globalization.CultureInfo.InvariantCulture);
                    i++;
                    break;
            }
        }

        return options;
    }
}
