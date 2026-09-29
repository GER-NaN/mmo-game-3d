namespace MmoGame3d.Overseer;

using System;
using System.Collections.Generic;

/// <summary>
/// The Overseer: runs bots, each in its own game client, and gathers what they report.
/// See docs/features/bots.md and this folder's README.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        string godot = "";
        string project = "";
        List<BotSpec> bots = new List<BotSpec>();
        double timeoutSeconds = 120;

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
                default:
                    Console.Error.WriteLine("Unknown option: " + args[i]);
                    return 2;
            }
        }

        if (godot.Length == 0 || project.Length == 0)
        {
            Console.Error.WriteLine("Usage: Overseer --godot <Godot exe> --project <project folder> [--bot <activity>[:connect|:fresh]]... [--timeout seconds]");
            return 2;
        }

        if (bots.Count == 0)
        {
            bots.Add(new BotSpec("main-menu", BotPlayer.None));
        }

        BotRun run = new BotRun(godot, project, bots, TimeSpan.FromSeconds(timeoutSeconds));
        return run.Run();
    }
}
