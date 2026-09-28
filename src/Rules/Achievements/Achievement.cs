namespace MmoGame3d.Rules.Achievements;
public class Achievement
{
    public Achievement(string id, string title, string text)
    {
        Id = id;
        Title = title;
        Text = text;
    }

    public string Id { get; }
    public string Title { get; }
    public string Text { get; }
}
