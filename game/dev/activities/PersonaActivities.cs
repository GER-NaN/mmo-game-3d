namespace MmoGame3d.Dev.Activities;
/// <summary>
/// The activities only some personas do (BotPersonas, Own): poking at every screen,
/// running for the edge of the world, squeezing into gaps, mashing keys, shadowing
/// someone, saying odd things.
/// </summary>
public static class PersonaActivities
{
    public static readonly BotActivity PokeAround = new PokeAroundActivity();
    public static readonly BotActivity PokeAtTerminal = new PokeAtTerminalActivity();
    public static readonly BotActivity RunForTheEdge = new RunForTheEdgeActivity();
    public static readonly BotActivity MashKeys = new MashKeysActivity();
    public static readonly BotActivity SqueezeIntoAGap = new SqueezeIntoAGapActivity();
    public static readonly BotActivity ShadowSomeone = new ShadowSomeoneActivity();
    public static readonly BotActivity SaySomethingOdd = new SaySomethingOddActivity();
}
