namespace MmoGame3d.Bots;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// The bot's events file: one JSON line per event, in the bot's execution folder. The
/// Overseer reads it while the bot runs. Each line is flushed at once, so a crash keeps
/// everything written before it.
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

    public void Write(string kind, string detail)
    {
        Dictionary<string, string> line = new Dictionary<string, string>
        {
            { "time", DateTime.UtcNow.ToString("o") },
            { "kind", kind },
            { "detail", detail },
        };

        _writer.WriteLine(JsonSerializer.Serialize(line));
    }

    public void Close()
    {
        _writer.Dispose();
    }
}
