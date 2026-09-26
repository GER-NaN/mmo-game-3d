namespace MmoGame3d.Rules.Town;

/// <summary>
/// What townspeople say when spoken to. world.md: they are blissfully ignorant. They know
/// about the AI from the news and blow it off, and they do not see the fight. Placeholder
/// lines; each person picks one at random.
/// </summary>
public static class Chatter
{
    private static readonly string[] Lines =
    {
        "They said on the news the AI is 'contained'. Good enough for me.",
        "My fridge ordered forty cartons of milk last night. Probably a glitch.",
        "Lovely day. The street lights were out again, though.",
        "I don't trust those robo taxis. Took me the long way round twice.",
        "Hackers? In this town? Nothing ever happens here.",
        "The TV in the shop window keeps showing the same face. Odd advert.",
        "If you're looking for parts, try the woods north of town.",
    };

    public static string Pick(Random random)
    {
        return Lines[random.Next(Lines.Length)];
    }

    public static int Count
    {
        get { return Lines.Length; }
    }
}
