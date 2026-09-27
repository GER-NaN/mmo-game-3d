namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Where the bots' judges write what they find: one JSON line each in the soak folder's
/// judge/findings.jsonl (who, when, what kind, the judge's own fields), and a picture
/// of the game view beside it. tools/soak-watch/judge_report.py reads them.
/// </summary>
public static class BotFindings
{
    public static readonly string Folder = Path.Combine(Path.GetTempPath(), "mmo-game-3d-bots", "judge");

    public static void Write(Player me, string profile, string kind, string detail, Dictionary<string, object?> fields)
    {
        Directory.CreateDirectory(Path.Combine(Folder, "shots"));
        string shot = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + profile + "-" + kind + ".png";
        me.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(Folder, "shots", shot));

        Dictionary<string, object?> record = new Dictionary<string, object?>
        {
            { "ts", DateTime.UtcNow.ToString("o") },
            { "bot", profile },
            { "player", me.DisplayName },
            { "peer", me.Name.ToString() },
            { "kind", kind },
            { "detail", detail },
            { "shot", shot },
        };

        foreach (KeyValuePair<string, object?> field in fields)
        {
            record[field.Key] = field.Value;
        }

        File.AppendAllText(Path.Combine(Folder, "findings.jsonl"), JsonSerializer.Serialize(record) + "\n");
        GD.Print("Judge: " + kind + ": " + detail + " [" + shot + "]");
    }
}
