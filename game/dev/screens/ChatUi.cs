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

    // One of the lines people say in passing, now and then.
    public static BotStep SayInPassing(System.Random random)
    {
        return Say(InPassing[random.Next(InPassing.Length)]);
    }

    private static readonly string[] InPassing =
    {
        "anyone seen a battery?",
        "this town needs more lights",
        "hello",
        "found some RAM over here",
        "the drones are out again",
        "lfg substation repair",
    };
}
