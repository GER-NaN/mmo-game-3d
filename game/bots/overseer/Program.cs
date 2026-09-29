namespace MmoGame3d.Overseer;

using System;
using System.Collections.Generic;

/// <summary>
/// The Overseer: runs bots, each in its own game client, and gathers what they report.
/// See docs/features/bots.md and this folder's README.
/// </summary>
public static class Program
{
    private const string Usage = "Usage: Overseer --godot <Godot exe> --project <project folder> [--bot <activity>|@<persona>[:connect|:fresh]]... [--timeout seconds] [--duration seconds] [--seed n]";

    public static int Main(string[] args)
    {
        string godot = "";
        string project = "";
        List<BotSpec> bots = new List<BotSpec>();
        double timeoutSeconds = 120;
        double durationSeconds = 0;
        int seed = Environment.TickCount & 0xFFFFF;

        for (int i = 0; i < args.Length; i++)
        {
            string next = i + 1 < args.Length ? args[i + 1] : "";

            switch (args[i])
            {
                case "--godot":
                    godot = next;
                    i++;
                    break;
                case "--project":
                    project = next;
                    i++;
                    break;
                case "--bot":
                    bots.Add(BotSpec.Parse(next));
                    i++;
                    break;
                case "--timeout":
                    timeoutSeconds = double.Parse(next);
                    i++;
                    break;
                case "--duration":
                    durationSeconds = double.Parse(next);
                    i++;
                    break;
                case "--seed":
                    seed = int.Parse(next);
                    i++;
                    break;
                default:
                    Console.Error.WriteLine("Unknown option: " + args[i]);
                    Console.Error.WriteLine(Usage);
                    return 2;
            }
        }

        if (godot.Length == 0 || project.Length == 0)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        if (bots.Count == 0)
        {
            bots.Add(BotSpec.Parse("main-menu"));
        }

        // A persona plays until stopped: without a duration, the timeout is its duration.
        double duration = durationSeconds > 0 ? durationSeconds : timeoutSeconds;
        BotRun run = new BotRun(godot, project, bots, TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromSeconds(duration), seed);
        return run.Run();
    }
}
