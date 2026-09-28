namespace MmoGame3d.Dev.Screens;
/// <summary>The panels opened by their own keys: the map, skills, friends.</summary>
public static class LookUi
{
    public static BotStep OpenMap()
    {
        return ScreenSteps.Press("open the map", "map");
    }

    public static BotStep OpenSkills()
    {
        return ScreenSteps.Press("open skills", "skills");
    }

    public static BotStep OpenFriends()
    {
        return ScreenSteps.Press("open friends", "social");
    }
}
