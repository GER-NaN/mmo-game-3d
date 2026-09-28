namespace MmoGame3d.Overseer;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

/// <summary>
/// One bot in one game client, from start to exit. Everything about it is in its own
/// folder: the events file the bot writes, the client's log, and the stop file this
/// leaves to ask the bot to quit.
/// </summary>
public class BotExecution
{
    private const string EventsFile = "events.jsonl";
    private const string StopFile = "stop";

    private readonly Process _process;
    private readonly string _eventsPath;
    private long _readTo;
    private string _partialLine = "";

    private BotExecution(string name, string folder, Process process)
    {
        Name = name;
        Folder = folder;
        _process = process;
        _eventsPath = Path.Combine(folder, EventsFile);
    }

    public string Name { get; }

    public string Folder { get; }

    public bool HasExited
    {
        get { return _process.HasExited; }
    }

    public int ExitCode
    {
        get { return _process.ExitCode; }
    }

    // scene: the bot's scene in game/bots/, without ".tscn". profile: the player the
    // client connects as at once, or null to stay at the main menu ("fresh" is a new
    // player every launch).
    public static BotExecution Start(string godot, string project, string scene, string name, string folder, string? profile)
    {
        Directory.CreateDirectory(folder);
        WriteBotFile(folder, name, scene);

        ProcessStartInfo start = new ProcessStartInfo(godot);
        start.UseShellExecute = false;
        start.ArgumentList.Add("--path");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("--scene");
        start.ArgumentList.Add("res://game/bots/" + scene + ".tscn");
        start.ArgumentList.Add("--audio-driver");
        start.ArgumentList.Add("Dummy");
        start.ArgumentList.Add("--log-file");
        start.ArgumentList.Add(Path.Combine(folder, "client.log"));

        // After "--": the game's own options (game/LaunchOptions.cs), then the bot's.
        // The execution's own settings file, so the machine's is never read or written.
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--windowed");
        start.ArgumentList.Add("--settings-file");
        start.ArgumentList.Add(Path.Combine(folder, "settings.cfg"));

        if (profile != null)
        {
            start.ArgumentList.Add("--autoconnect");
            start.ArgumentList.Add("--profile");
            start.ArgumentList.Add(profile);
        }

        start.ArgumentList.Add("--bot-folder");
        start.ArgumentList.Add(folder);

        // The client's output is already in client.log. It is drained and dropped, so it
        // neither fills the Overseer's console nor blocks the client on a full pipe.
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;

        Process process = Process.Start(start)!;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return new BotExecution(name, folder, process);
    }

    // The lines written since the last call. A line still being written waits for the
    // next call.
    public List<BotEvent> ReadNewEvents()
    {
        List<BotEvent> events = new List<BotEvent>();

        if (!File.Exists(_eventsPath))
        {
            return events;
        }

        string text;

        // The bot holds the file open for writing, so it is shared on read.
        using (FileStream stream = new FileStream(_eventsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            stream.Seek(_readTo, SeekOrigin.Begin);

            // Disposing the reader closes the stream, so the position is taken inside.
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                text = reader.ReadToEnd();
                _readTo = stream.Position;
            }
        }

        string[] lines = (_partialLine + text).Split('\n');
        _partialLine = lines[lines.Length - 1];

        for (int i = 0; i < lines.Length - 1; i++)
        {
            string line = lines[i].Trim();

            if (line.Length > 0)
            {
                events.Add(Parse(line));
            }
        }

        return events;
    }

    public void RequestStop()
    {
        File.WriteAllText(Path.Combine(Folder, StopFile), "");
    }

    public void Kill()
    {
        if (!_process.HasExited)
        {
            _process.Kill();
            _process.WaitForExit();
        }
    }

    // bot.json: what this execution is. The bot reads its configuration from here as it
    // grows; for now it only names the execution and its scene.
    private static void WriteBotFile(string folder, string name, string scene)
    {
        Dictionary<string, string> bot = new Dictionary<string, string>
        {
            { "name", name },
            { "scene", scene },
        };

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(folder, "bot.json"), JsonSerializer.Serialize(bot, options));
    }

    private static BotEvent Parse(string line)
    {
        using (JsonDocument json = JsonDocument.Parse(line))
        {
            JsonElement root = json.RootElement;
            return new BotEvent(Field(root, "time"), Field(root, "kind"), Field(root, "detail"));
        }
    }

    private static string Field(JsonElement root, string name)
    {
        JsonElement value;
        return root.TryGetProperty(name, out value) ? value.GetString() ?? "" : "";
    }
}
