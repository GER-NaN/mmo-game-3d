namespace MmoGame3d.Overseer;

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

/// <summary>
/// One run: starts the bots, watches their events until they are done, and leaves each
/// execution's folder under bot-runs/ as the report. For now one bot. It passes when it
/// reported "done" and its client quit with exit code 0.
/// </summary>
public class BotRun
{
    private const int PollMilliseconds = 250;
    private const int ServerPort = 7070;

    // How long a bot has to quit by itself after its stop file, before it is killed.
    private static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ServerStartLimit = TimeSpan.FromSeconds(30);

    private readonly string _godot;
    private readonly string _project;
    private readonly string _scene;
    private readonly bool _connect;
    private readonly bool _fresh;
    private readonly TimeSpan _timeout;

    // fresh: connect as a new player (the "fresh" profile), not the kept player "bot1".
    public BotRun(string godot, string project, string scene, bool connect, bool fresh, TimeSpan timeout)
    {
        _godot = godot;
        _project = project;
        _scene = scene;
        _connect = connect || fresh;
        _fresh = fresh;
        _timeout = timeout;
    }

    public int Run()
    {
        if (_connect && !EnsureServer())
        {
            Console.WriteLine("Run failed: no server.");
            return 1;
        }

        string folder = MakeRunFolder();
        Console.WriteLine("Run folder: " + folder);

        string? profile = _connect ? (_fresh ? "fresh" : "bot1") : null;
        BotExecution bot = BotExecution.Start(_godot, _project, _scene, "bot1", Path.Combine(folder, "bot1"), profile);

        // Whatever happens here, no client is left running after the run.
        try
        {
            return Watch(bot);
        }
        finally
        {
            bot.Kill();
        }
    }

    private int Watch(BotExecution bot)
    {
        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan? stopAskedAt = null;
        bool done = false;
        bool killed = false;

        while (!bot.HasExited)
        {
            if (ReadEvents(bot))
            {
                done = true;
            }

            if (stopAskedAt == null && (done || clock.Elapsed > _timeout))
            {
                if (!done)
                {
                    Console.WriteLine("[" + bot.Name + "] not done after " + _timeout.TotalSeconds + " s");
                }

                bot.RequestStop();
                stopAskedAt = clock.Elapsed;
            }

            if (stopAskedAt != null && clock.Elapsed - stopAskedAt.Value > StopGrace)
            {
                Console.WriteLine("[" + bot.Name + "] did not quit after its stop file; killed");
                bot.Kill();
                killed = true;
                break;
            }

            Thread.Sleep(PollMilliseconds);
        }

        if (ReadEvents(bot))
        {
            done = true;
        }

        if (!killed)
        {
            Console.WriteLine("[" + bot.Name + "] exited with code " + bot.ExitCode);
        }

        bool passed = done && !killed && bot.ExitCode == 0;
        Console.WriteLine(passed ? "Run passed." : "Run failed.");
        return passed ? 0 : 1;
    }

    // Prints the bot's new events; true when one of them is "done".
    private static bool ReadEvents(BotExecution bot)
    {
        bool done = false;

        foreach (BotEvent botEvent in bot.ReadNewEvents())
        {
            string detail = botEvent.Detail.Length > 0 ? " " + botEvent.Detail : "";
            Console.WriteLine("[" + bot.Name + "] " + botEvent.Kind + detail);

            if (botEvent.Kind == "done")
            {
                done = true;
            }
        }

        return done;
    }

    // The main server, as scripts/server-up.ps1 starts it, if nothing listens on its port.
    // It is left running after the run; scripts/server-stop.ps1 stops it so it saves.
    private bool EnsureServer()
    {
        if (ServerListening())
        {
            Console.WriteLine("Server: already running");
            return true;
        }

        Console.WriteLine("Server: starting");
        ProcessStartInfo start = new ProcessStartInfo(_godot);
        start.UseShellExecute = true;
        start.ArgumentList.Add("--headless");
        start.ArgumentList.Add("--path");
        start.ArgumentList.Add(_project);
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--server");
        Process.Start(start);

        Stopwatch clock = Stopwatch.StartNew();

        while (clock.Elapsed < ServerStartLimit)
        {
            if (ServerListening())
            {
                Console.WriteLine("Server: up after " + (int)clock.Elapsed.TotalSeconds + " s");
                return true;
            }

            Thread.Sleep(PollMilliseconds);
        }

        Console.WriteLine("Server: not listening after " + ServerStartLimit.TotalSeconds + " s");
        return false;
    }

    // ENet is UDP, so a server is up when something holds its UDP port.
    private static bool ServerListening()
    {
        foreach (IPEndPoint endPoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
        {
            if (endPoint.Port == ServerPort)
            {
                return true;
            }
        }

        return false;
    }

    // bot-runs/<time>/, with a .gdignore beside the runs so Godot never imports their
    // pictures into the project.
    private string MakeRunFolder()
    {
        string runs = Path.Combine(_project, "bot-runs");
        Directory.CreateDirectory(runs);
        string ignore = Path.Combine(runs, ".gdignore");

        if (!File.Exists(ignore))
        {
            File.WriteAllText(ignore, "");
        }

        string folder = Path.Combine(runs, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(folder);
        return folder;
    }
}
