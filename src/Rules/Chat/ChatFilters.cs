namespace MmoGame3d.Rules.Chat;

using System.Text;

/// <summary>
/// One rule applied to a chat line on the server, before anyone sees it. Text in, text
/// out, no state, so rules compose and test on their own. An empty result drops the line.
/// </summary>
public interface IChatFilter
{
    string Apply(string text);
}

/// <summary>
/// Every rule in order. This is the seam word lists and per-player mutes hang off later.
/// </summary>
public class ChatFilterPipeline
{
    private readonly List<IChatFilter> _filters;

    public ChatFilterPipeline(IEnumerable<IChatFilter> filters)
    {
        _filters = new List<IChatFilter>(filters);
    }

    public static ChatFilterPipeline Default()
    {
        return new ChatFilterPipeline(new IChatFilter[] { new CleanFilter() });
    }

    public string Apply(string text)
    {
        foreach (IChatFilter filter in _filters)
        {
            text = filter.Apply(text);

            if (text.Length == 0)
            {
                break;
            }
        }

        return text;
    }
}

/// <summary>
/// Makes a line safe to show and short enough for one line of chat: control characters
/// out (a newline would forge a second line), runs of spaces to one, the ends trimmed,
/// and the rest cut at the maximum length.
/// </summary>
public class CleanFilter : IChatFilter
{
    public const int MaxLength = 120;

    public string Apply(string text)
    {
        StringBuilder clean = new StringBuilder(text.Length);
        bool lastWasSpace = false;

        foreach (char c in text)
        {
            bool space = char.IsWhiteSpace(c) || char.IsControl(c);

            if (space)
            {
                if (!lastWasSpace)
                {
                    clean.Append(' ');
                }
            }
            else
            {
                clean.Append(c);
            }

            lastWasSpace = space;
        }

        string trimmed = clean.ToString().Trim();

        if (trimmed.Length <= MaxLength)
        {
            return trimmed;
        }

        // Not through the middle of an emoji (two chars): half of one is not text.
        int cut = char.IsHighSurrogate(trimmed[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
        return trimmed.Substring(0, cut);
    }
}
