namespace MmoGame3d.Rules.Chat;
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
