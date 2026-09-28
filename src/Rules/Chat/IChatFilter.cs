namespace MmoGame3d.Rules.Chat;
/// <summary>
/// One rule applied to a chat line on the server, before anyone sees it. Text in, text
/// out, no state, so rules compose and test on their own. An empty result drops the line.
/// </summary>
public interface IChatFilter
{
    string Apply(string text);
}
