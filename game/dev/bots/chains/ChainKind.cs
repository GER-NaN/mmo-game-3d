namespace MmoGame3d.Dev;
public enum ChainKind
{
    // Written by hand around one piece of state: enroll, change career, enroll back.
    Related,

    // Drawn at random, three to six, with its own seed so it can be run again exactly.
    Random,

    // Planned from facts, and planned again after every activity (BotResolver).
    Goal,
}
