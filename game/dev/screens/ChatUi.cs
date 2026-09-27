namespace MmoGame3d.Dev.Screens;

using Godot;

/// <summary>
/// The chat line: opened with its key, a line typed, Enter. Talk, or a command such as an
/// emote ("/wave"). The only bot code that types into chat.
/// </summary>
public static class ChatUi
{
    public static BotStep Say(string line)
    {
        return new DoStep("say \"" + line + "\"", 2, (b, d) =>
        {
            GD.Print("Bot: saying \"" + line + "\"");
            b.Chat(line);
            return StepResult.Done;
        });
    }
}
