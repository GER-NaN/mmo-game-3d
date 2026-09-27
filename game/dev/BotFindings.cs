namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Where the bots' judges put what they find, so a person can review it later without
/// anyone having watched: a folder per finding under the bot folder's judge/, holding
/// finding.json (what the judge saw), picture.png (the game view at that moment) and
/// client.log (the bot's last log lines). tools/bot-watch adds the server's side
/// (server.jsonl, server.txt) and rewrites judge/report.html, the page to review them
/// all from. judge/findings.jsonl lists every finding, one line each.
/// </summary>
public static class BotFindings
{
    public static readonly string Folder = Path.Combine(Path.GetTempPath(), "mmo-game-3d-bots", "judge");

    private const int ClientLogLines = 300;

    public static void Write(Player me, string profile, string kind, string detail, Dictionary<string, object?> fields)
    {
        string name = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + profile + "-" + kind;
        string folder = Path.Combine(Folder, name);
        Directory.CreateDirectory(folder);
        me.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(folder, "picture.png"));

        Dictionary<string, object?> record = new Dictionary<string, object?>
        {
            { "ts", DateTime.UtcNow.ToString("o") },
            { "bot", profile },
            { "player", me.DisplayName },
            { "peer", me.Name.ToString() },
            { "kind", kind },
            { "detail", detail },
            { "folder", name },
        };

        foreach (KeyValuePair<string, object?> field in fields)
        {
            record[field.Key] = field.Value;
        }

        File.WriteAllText(Path.Combine(folder, "finding.json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(folder, "client.log"), LastLines(Path.Combine(Path.GetTempPath(), "mmo-game-3d-bots", profile + ".log")));
        File.AppendAllText(Path.Combine(Folder, "findings.jsonl"), JsonSerializer.Serialize(record) + "\n");
        GD.Print("Judge: " + kind + ": " + detail + " [" + name + "]");
    }

    // The end of the bot's own log (scripts/bots-up.ps1 names it after the profile),
    // read while the game still writes it.
    private static string LastLines(string path)
    {
        if (!File.Exists(path))
        {
            return "(no log at " + path + ")";
        }

        using (FileStream stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite))
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            List<string> lines = new List<string>();
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);

                if (lines.Count > ClientLogLines)
                {
                    lines.RemoveAt(0);
                }
            }

            return string.Join("\n", lines) + "\n";
        }
    }
}
