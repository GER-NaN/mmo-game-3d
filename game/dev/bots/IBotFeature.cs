namespace MmoGame3d.Dev;
/// <summary>
/// A game feature's part in bot testing, in one file: the activities and chains that play
/// it. Every class that implements this is found when bots start (BotCatalog) and adds
/// its own; nothing else needs changing. See game/dev/bots/features/.
/// </summary>
public interface IBotFeature
{
    void AddTo(BotCatalog catalog);
}
