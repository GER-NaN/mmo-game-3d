namespace MmoGame3d.Rules.Events;
// How a world event ended. The swarm reaching its own goal (destroying something,
// hacking a thing) comes later, as another outcome (world.md section 2).
public enum WorldEventOutcome
{
    Completed,
    TimedOut,
    EndedByRestart,
}
