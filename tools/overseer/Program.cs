namespace MmoGame3d.Overseer;

using System;

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
        string scene = "BotMain";
        bool connect = false;
        bool fresh = false;
        double timeoutSeconds = 30;

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
                    scene = next;
                    i++;
                    break;
                case "--connect":
                    connect = true;
                    break;
                case "--fresh":
                    fresh = true;
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
            Console.Error.WriteLine("Usage: Overseer --godot <Godot exe> --project <project folder> [--bot <scene in game/bots>] [--connect] [--fresh] [--timeout seconds]");
            return 2;
        }

        BotRun run = new BotRun(godot, project, scene, connect, fresh, TimeSpan.FromSeconds(timeoutSeconds));
        return run.Run();
    }
}
