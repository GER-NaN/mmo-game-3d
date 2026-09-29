namespace MmoGame3d.Bots;

using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// What this bot is to do, from its execution folder's bot.json (the Overseer writes it):
/// one activity to play once, or a persona to play until stopped; and the seed every
/// random choice comes from, so a run can be played again.
/// </summary>
public class BotSetup
{
    public const string FileName = "bot.json";

    private BotSetup(string activity, string persona, int seed)
    {
        Activity = activity;
        Persona = persona;
        Seed = seed;
    }

    // Empty unless the bot plays one activity.
    public string Activity { get; }

    // Empty unless the bot plays a persona.
    public string Persona { get; }

    public int Seed { get; }

    public static BotSetup Read(string folder)
    {
        string path = Path.Combine(folder, FileName);

        if (!File.Exists(path))
        {
            return new BotSetup("", "", Environment.TickCount);
        }

        using (JsonDocument json = JsonDocument.Parse(File.ReadAllText(path)))
        {
            JsonElement root = json.RootElement;
            JsonElement seed;
            int seedValue = root.TryGetProperty("seed", out seed) && seed.ValueKind == JsonValueKind.Number ? seed.GetInt32() : Environment.TickCount;
            return new BotSetup(Text(root, "activity"), Text(root, "persona"), seedValue);
        }
    }

    private static string Text(JsonElement root, string name)
    {
        JsonElement value;
        return root.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    }
}
