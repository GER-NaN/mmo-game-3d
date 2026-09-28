namespace MmoGame3d.BotJudging;
/// <summary>
/// An emote typed in chat, as its player sees it: with nothing in the way (not online, no
/// screen open) the body does it; online, where the server holds the body still, it does
/// not. With a screen open the chat key may not reach the chat line, so that is not judged;
/// nor is an emote typed as the body went through a door (a new body in a new zone).
/// </summary>
public static class EmoteCheck
{
    public static string? Judge(string emote, bool clear, bool online, bool seen, bool zoneChanged = false)
    {
        if (zoneChanged)
        {
            return null;
        }

        if (clear && !seen)
        {
            return "typed /" + emote + " with nothing in the way, and the body did not do it within 2 s";
        }

        if (online && seen)
        {
            return "typed /" + emote + " while online, and the body did it";
        }

        return null;
    }
}
