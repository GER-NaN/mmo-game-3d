namespace MmoGame3d.Overseer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

/// <summary>
/// One run: starts every bot at once, each in its own client, watches their events, and
/// leaves each execution's folder under bot-runs/ as the report.
///
/// A one-activity bot is stopped once it says "done" or "failed", or at the timeout; it
/// passes when it said "done" and quit cleanly. A persona bot plays until the run's
/// duration is up; if its client dies before, it is started again as the same player.
/// A "stop" file in the run's folder (scripts/bots-stop.ps1) stops every bot. The run
/// passes when every bot passed.
/// </summary>
public class BotRun
{
    private const int PollMilliseconds = 250;

    // How long a bot has to quit by itself after its stop file, before it is killed.
    private static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(10);

    // What a persona bot's lines show on the console; the rest is in its events file.
    private static readonly HashSet<string> SoakShows = new HashSet<string> { "activity", "completed", "failed", "walked-away", "finding", "keeper" };

    private readonly string _godot;
    private readonly string _project;
    private readonly List<BotSpec> _bots;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _duration;
    private readonly int _seed;
    private readonly List<BotExecution> _executions = new List<BotExecution>();
    private readonly Dictionary<BotExecution, TimeSpan> _startedAt = new Dictionary<BotExecution, TimeSpan>();
    private readonly Stopwatch _clock = new Stopwatch();
    private string _folder = "";

    public BotRun(string godot, string project, List<BotSpec> bots, TimeSpan timeout, TimeSpan duration, int seed)
    {
        _godot = godot;
        _project = project;
        _bots = bots;
        _timeout = timeout;
        _duration = duration;
        _seed = seed;
    }

    public int Run()
    {
        if (_bots.Exists(spec => spec.Player != BotPlayer.None) && !LocalServer.Ensure(_godot, _project))
        {
            Console.WriteLine("Run failed: no server.");
            return 1;
        }

        _folder = MakeRunFolder();
        Console.WriteLine("Run folder: " + _folder + ", seed " + _seed);
        _clock.Start();

        // Whatever happens here, no client is left running after the run.
        try
        {
            for (int i = 0; i < _bots.Count; i++)
            {
                Start(_bots[i], UniqueName(_bots[i].Name), i, 0);
            }

            Watch();
            return Report();
        }
        finally
        {
            foreach (BotExecution bot in _executions)
            {
                bot.Kill();
            }
        }
    }

    private void Start(BotSpec spec, string name, int index, int restart)
    {
        string folderName = restart == 0 ? name : name + "-r" + restart;
        int seed = _seed + (index * 1000) + restart;
        BotExecution bot = BotExecution.Start(_godot, _project, spec, name, Path.Combine(_folder, folderName), Profile(spec, name), seed);
        _executions.Add(bot);
        _startedAt[bot] = _clock.Elapsed;
    }

    private void Watch()
    {
        while (_executions.Exists(bot => !bot.HasExited))
        {
            bool stopAll = File.Exists(Path.Combine(_folder, "stop"));

            // Restarts add to the list, so it is walked by index.
            for (int i = 0; i < _executions.Count; i++)
            {
                BotExecution bot = _executions[i];

                if (bot.HasExited)
                {
                    continue;
                }

                ReadEvents(bot);
                TimeSpan elapsed = _clock.Elapsed;
                bool due = bot.Spec.IsPersona ? elapsed > _duration : elapsed - _startedAt[bot] > _timeout || bot.Done || bot.Failed;

                if (bot.StopAskedAt == null && (due || stopAll))
                {
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

            RestartTheDead(stopAll);
            Thread.Sleep(PollMilliseconds);
        }
    }

    // A persona bot whose client died without being asked: once more, as the same player.
    private void RestartTheDead(bool stopAll)
    {
        int count = _executions.Count;

        for (int i = 0; i < count; i++)
        {
            BotExecution bot = _executions[i];

            if (!bot.Spec.IsPersona || !bot.HasExited || bot.StopAskedAt != null || stopAll || _clock.Elapsed > _duration)
            {
                continue;
            }

            ReadEvents(bot);
            int restarts = _executions.FindAll(other => other.Name == bot.Name).Count;
            Console.WriteLine("[" + bot.Name + "] client ended by itself (exit code " + bot.ExitCode + "); starting it again");
            bot.StopAskedAt = _clock.Elapsed;
            bot.Killed = true;
            Start(bot.Spec, bot.Name, _bots.IndexOf(bot.Spec), restarts);
        }
    }

    // One line per bot, and whether the run passed.
    private int Report()
    {
        Console.WriteLine();
        int passed = 0;

        foreach (BotExecution bot in _executions)
        {
            ReadEvents(bot);
            string how = bot.Killed ? "killed" : "exit code " + bot.ExitCode;
            string counts = bot.Spec.IsPersona
                ? ", " + bot.Count("activity") + " activities: " + bot.Count("completed") + " completed, " + bot.Count("failed") + " failed, " + bot.Count("walked-away") + " walked away; " + bot.Count("finding") + " findings"
                : "";
            Console.WriteLine((bot.Passed ? "  passed  " : "  FAILED  ") + bot.Name + " (" + how + counts + ")");

            if (bot.Passed)
            {
                passed++;
            }
        }

        Console.WriteLine(passed == _executions.Count ? "Run passed." : "Run failed: " + passed + " of " + _executions.Count + " passed.");
        return passed == _executions.Count ? 0 : 1;
    }

    // Prints the bot's new events (a persona bot's, only the ones that matter), and notes
    // "done" and "failed" among them.
    private static void ReadEvents(BotExecution bot)
    {
        foreach (BotEvent botEvent in bot.ReadNewEvents())
        {
            if (!bot.Spec.IsPersona || SoakShows.Contains(botEvent.Kind))
            {
                string detail = botEvent.Detail.Length > 0 ? " " + botEvent.Detail : "";
                Console.WriteLine("[" + bot.Name + "] " + botEvent.Kind + detail);
            }

            if (botEvent.Kind == "done")
            {
                bot.Done = true;
            }
            else if (botEvent.Kind == "failed" && !bot.Spec.IsPersona)
            {
                bot.Failed = true;
            }
        }
    }

    // A kept player is named after its bot, so each bot is its own player, the same one
    // on every run.
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

    // The bot's name, with a number after it when the run has it more than once.
    private string UniqueName(string name)
    {
        string unique = name;
        int number = 2;

        while (_executions.Exists(bot => bot.Name == unique))
        {
            unique = name + "-" + number;
            number++;
        }

        return unique;
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
