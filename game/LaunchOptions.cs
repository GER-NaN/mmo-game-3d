namespace MmoGame3d;

using MmoGame3d.Data;

/// <summary>
/// What this run of the game is, from the arguments after "--" on the command line.
///
///   --server            run as the headless server
///   --port 7070         the server's port (both sides)
///   --db "Host=..."     the server's database
///   --max-players 300   how many clients the server accepts
///   --profile name      which player this client is (default "default"; "fresh" is a
///                       new player every launch)
///   --name Gerald       the display name for a new player
///   --address 1.2.3.4   the server to connect to
///   --autoconnect       skip the main menu and connect at once
/// </summary>
public class LaunchOptions
{
    public const int DefaultPort = 7070;

    public bool IsServer { get; private set; }
    public int Port { get; private set; } = DefaultPort;
    public string DatabaseConnection { get; private set; } = Database.DefaultConnectionString;
    public int MaxPlayers { get; private set; } = 300;
    public string Profile { get; private set; } = "default";
    public string? DisplayName { get; private set; }
    public string? Address { get; private set; }
    public bool AutoConnect { get; private set; }

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
            }
        }

        return options;
    }
}
