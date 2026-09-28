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
        bool opened = false;
        return new DoStep("say \"" + line + "\"", 3, (b, d) =>
        {
            if (!opened)
            {
                opened = true;
                GD.Print("Bot: saying \"" + line + "\"");
                b.Chat(line);
                return StepResult.Running;
            }

            // Done once typed (or given up on): the next step starts after the line is out.
            return b.Chatting ? StepResult.Running : StepResult.Done;
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
