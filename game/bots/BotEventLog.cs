namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// The bot's events file: one JSON line per event, in the bot's execution folder. The
/// Overseer reads it while the bot runs. Each line carries the activity and step it
/// happened in, and is flushed at once, so a crash keeps everything written before it.
/// </summary>
public class BotEventLog
{
    public const string FileName = "events.jsonl";

    private readonly StreamWriter _writer;

    public BotEventLog(string folder)
    {
        Directory.CreateDirectory(folder);
        _writer = new StreamWriter(Path.Combine(folder, FileName), true);
        _writer.AutoFlush = true;
    }

    // What the bot is doing now; the runner keeps these current.
    public string Activity { get; set; } = "";

    public string Step { get; set; } = "";

    public void Write(string kind, string detail)
    {
        Dictionary<string, string> line = new Dictionary<string, string>
        {
            { "time", DateTime.UtcNow.ToString("o") },
            { "kind", kind },
            { "detail", detail },
        };

        if (Activity.Length > 0)
        {
            line["activity"] = Activity;
        }

        if (Step.Length > 0)
        {
            line["step"] = Step;
        }

        _writer.WriteLine(JsonSerializer.Serialize(line));
    }

    public void Close()
    {
        _writer.Dispose();
    }
}
