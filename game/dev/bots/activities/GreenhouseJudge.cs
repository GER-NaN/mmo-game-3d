namespace MmoGame3d.Dev.Activities;

using MmoGame3d.BotJudging;
using MmoGame3d.Dev.Screens;
using MmoGame3d.Gardening;

/// <summary>
/// The plant as its maker sees it: pieces stay on the soil (the table's count goes up), and
/// the table says the plant was made. A cancel only has to leave no half-made plant behind
/// the table's back, which a player cannot see, so a cancelled run is not judged.
/// </summary>
public sealed class GreenhouseJudge : BotActivityJudge
{
    private int _mostPieces;
    private bool _madeSeen;
    private string _status = "";

    public override string? Watch(BotBody body, double delta)
    {
        GardenScreen? screen = GardenUi.Screen(body);

        if (screen != null)
        {
            _mostPieces = System.Math.Max(_mostPieces, screen.PieceCount);
            _madeSeen = _madeSeen || screen.IsDone;
            _status = screen.Status;
        }

        return null;
    }

    public override string? After(BotBody body, BotEnd end)
    {
        return PlantCheck.Judge(end == BotEnd.Finished, end == BotEnd.Cancelled, _madeSeen, _mostPieces, GardenUi.PieceKinds(body) > 0, _status);
    }
}
