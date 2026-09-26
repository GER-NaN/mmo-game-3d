namespace MmoGame3d.Client;

using MmoGame3d.Rules.Chat;

// One line of chat as the client keeps it, so a screen opened later can show the past.
public class ChatLine
{
    public ChatLine(string sender, string text, ChatKind kind)
    {
        Sender = sender;
        Text = text;
        Kind = kind;
    }

    public string Sender { get; }
    public string Text { get; }
    public ChatKind Kind { get; }
}
