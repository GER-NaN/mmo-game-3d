namespace MmoGame3d.Overseer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

/// <summary>
/// One bot in one game client, from start to exit. Everything about it is in its own
/// folder: bot.json (what it plays, its seed), the events file it writes, the client's
/// log, and the stop file this leaves to ask it to quit. It counts the bot's events by
/// kind as they are read, for the run's summary.
/// </summary>
public class BotExecution
{
    public const string Scene = "res://game/bots/BotMain.tscn";

    private const string EventsFile = "events.jsonl";
    private const string StopFile = "stop";

    // DisplayName.MaxLength in src/Rules/Players.
    private const int MaxNameLength = 16;

    private readonly Process _process;
    private readonly string _eventsPath;
    private long _readTo;
    private string _partialLine = "";

    private BotExecution(BotSpec spec, string name, string folder, int seed, Process process)
    {
        Spec = spec;
        Name = name;
        Folder = folder;
        Seed = seed;
        _process = process;
        _eventsPath = Path.Combine(folder, EventsFile);
    }

    public BotSpec Spec { get; }

    public string Name { get; }

    public string Folder { get; }

    public int Seed { get; }

    public bool HasExited
    {
        get { return _process.HasExited; }
    }

    public int ExitCode
    {
        get { return _process.ExitCode; }
    }

    // The bot's events so far, counted by kind ("completed", "finding").
    public Dictionary<string, int> Tally { get; } = new Dictionary<string, int>();

    // What the run has seen of it so far.
    public bool Done { get; set; }

    public bool Failed { get; set; }

    public bool Killed { get; set; }

    public TimeSpan? StopAskedAt { get; set; }

    // Its client ended itself on purpose (a dropped connection, "dropping").
    public bool Dropped { get; set; }

    // A one-activity bot passes when it said "done" and quit cleanly; a persona bot when
    // it quit cleanly once asked, whatever its activities made of it.
    public bool Passed
    {
        get
        {
            bool clean = !Killed && HasExited && ExitCode == 0;
            return Spec.IsPersona ? (clean && StopAskedAt != null) || Dropped : clean && Done && !Failed;
        }
    }

    public int Count(string kind)
    {
        int count;
        return Tally.TryGetValue(kind, out count) ? count : 0;
    }

    // profile: the player the client connects as at once, or null to stay at the main
    // menu ("fresh" is a new player every launch).
    public static BotExecution Start(string godot, string project, BotSpec spec, string name, string folder, string? profile, int seed)
    {
        Directory.CreateDirectory(folder);
        WriteBotFile(folder, name, spec, seed);

        ProcessStartInfo start = new ProcessStartInfo(godot);
        start.UseShellExecute = false;
        start.ArgumentList.Add("--path");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("--scene");
        start.ArgumentList.Add(Scene);
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

            // A new player's name is the profile's unless given, and a name is at most 16
            // characters.
            start.ArgumentList.Add("--name");
            start.ArgumentList.Add(name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name);
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
        return new BotExecution(spec, name, folder, seed, process);
    }

    // The lines written since the last call, each counted in the tally. A line still being
    // written waits for the next call.
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
                BotEvent botEvent = Parse(line);
                Tally[botEvent.Kind] = Count(botEvent.Kind) + 1;
                events.Add(botEvent);
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

    // bot.json: what this execution is, read by the bot (game/bots/BotSetup.cs).
    private static void WriteBotFile(string folder, string name, BotSpec spec, int seed)
    {
        Dictionary<string, object> bot = new Dictionary<string, object>
        {
            { "name", name },
            { "seed", seed },
        };

        if (spec.IsPersona)
        {
            bot["persona"] = spec.Persona;
        }
        else
        {
            bot["activity"] = spec.Activity;
        }

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(folder, "bot.json"), JsonSerializer.Serialize(bot, options));
    }

    private static BotEvent Parse(string line)
    {
        using (JsonDocument json = JsonDocument.Parse(line))
        {
            JsonElement root = json.RootElement;
            return new BotEvent(Field(root, "time"), Field(root, "kind"), Field(root, "detail"), Field(root, "activity"));
        }
    }

    private static string Field(JsonElement root, string name)
    {
        JsonElement value;
        return root.TryGetProperty(name, out value) ? value.GetString() ?? "" : "";
    }
}
