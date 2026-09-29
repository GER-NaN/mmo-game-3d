namespace MmoGame3d.Overseer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

/// <summary>
/// One run: starts every bot at once, each in its own client, watches their events
/// until each is done, and leaves each execution's folder under bot-runs/ as the
/// report. A bot passes when it reported "done" and its client quit with exit code 0;
/// "failed" stops it at once. The run passes when every bot passed.
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
    private readonly List<BotSpec> _bots;
    private readonly TimeSpan _timeout;

    public BotRun(string godot, string project, List<BotSpec> bots, TimeSpan timeout)
    {
        _godot = godot;
        _project = project;
        _bots = bots;
        _timeout = timeout;
    }

    public int Run()
    {
        bool connect = false;

        foreach (BotSpec spec in _bots)
        {
            connect = connect || spec.Player != BotPlayer.None;
        }

        if (connect && !EnsureServer())
        {
            Console.WriteLine("Run failed: no server.");
            return 1;
        }

        string folder = MakeRunFolder();
        Console.WriteLine("Run folder: " + folder);
        List<BotExecution> executions = new List<BotExecution>();

        // Whatever happens here, no client is left running after the run.
        try
        {
            foreach (BotSpec spec in _bots)
            {
                string name = UniqueName(executions, spec.Activity);
                executions.Add(BotExecution.Start(_godot, _project, spec.Activity, name, Path.Combine(folder, name), Profile(spec, name)));
            }

            return Watch(executions);
        }
        finally
        {
            foreach (BotExecution bot in executions)
            {
                bot.Kill();
            }
        }
    }

    private int Watch(List<BotExecution> executions)
    {
        Stopwatch clock = Stopwatch.StartNew();

        while (!AllEnded(executions))
        {
            foreach (BotExecution bot in executions)
            {
                if (!bot.HasExited)
                {
                    Watch(bot, clock.Elapsed);
                }
            }

            Thread.Sleep(PollMilliseconds);
        }

        Console.WriteLine();
        int passed = 0;

        foreach (BotExecution bot in executions)
        {
            ReadEvents(bot);
            string how = bot.Killed ? "killed" : "exit code " + bot.ExitCode;
            Console.WriteLine((bot.Passed ? "  passed  " : "  FAILED  ") + bot.Name + " (" + how + ")");

            if (bot.Passed)
            {
                passed++;
            }
        }

        Console.WriteLine(passed == executions.Count ? "Run passed." : "Run failed: " + passed + " of " + executions.Count + " passed.");
        return passed == executions.Count ? 0 : 1;
    }

    private void Watch(BotExecution bot, TimeSpan elapsed)
    {
        ReadEvents(bot);

        if (bot.StopAskedAt == null && (bot.Done || bot.Failed || elapsed > _timeout))
        {
            if (!bot.Done && !bot.Failed)
            {
                Console.WriteLine("[" + bot.Name + "] not done after " + _timeout.TotalSeconds + " s");
            }

            bot.RequestStop();
            bot.StopAskedAt = elapsed;
        }

        if (bot.StopAskedAt != null && elapsed - bot.StopAskedAt.Value > StopGrace)
        {
            Console.WriteLine("[" + bot.Name + "] did not quit after its stop file; killed");
            bot.Kill();
            bot.Killed = true;
        }
    }

    private static bool AllEnded(List<BotExecution> executions)
    {
        foreach (BotExecution bot in executions)
        {
            if (!bot.HasExited)
            {
                return false;
            }
        }

        return true;
    }

    // Prints the bot's new events, and notes "done" and "failed" among them.
    private static void ReadEvents(BotExecution bot)
    {
        foreach (BotEvent botEvent in bot.ReadNewEvents())
        {
            string detail = botEvent.Detail.Length > 0 ? " " + botEvent.Detail : "";
            Console.WriteLine("[" + bot.Name + "] " + botEvent.Kind + detail);

            if (botEvent.Kind == "done")
            {
                bot.Done = true;
            }
            else if (botEvent.Kind == "failed")
            {
                bot.Failed = true;
            }
        }
    }

    // A kept player is named after its execution, so each bot in a run is its own player
    // and the same one on every run.
    private static string? Profile(BotSpec spec, string name)
    {
        switch (spec.Player)
        {
            case BotPlayer.Kept:
                return "bot-" + name;
            case BotPlayer.Fresh:
                return "fresh";
            default:
                return null;
        }
    }

    // The activity's name, with a number after it when the run has it more than once.
    private static string UniqueName(List<BotExecution> executions, string activity)
    {
        string name = activity;
        int number = 2;

        while (executions.Exists(bot => bot.Name == name))
        {
            name = activity + "-" + number;
            number++;
        }

        return name;
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

        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string folder = Path.Combine(runs, stamp);

        // Two runs started in the same second.
        for (int number = 2; Directory.Exists(folder); number++)
        {
            folder = Path.Combine(runs, stamp + "-" + number);
        }

        Directory.CreateDirectory(folder);
        return folder;
    }
}
