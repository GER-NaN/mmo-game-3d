namespace MmoGame3d.BotJudging;

/// <summary>
/// A plant at the potting table, as its maker sees it: finished means the table said the
/// plant was made; a failed run with no piece ever on the soil means the drags did not
/// land. A cancelled run is not judged.
/// </summary>
public static class PlantCheck
{
    public static string? Judge(bool finished, bool cancelled, bool madeSeen, int mostPieces, bool piecesOffered, string status)
    {
        if (cancelled)
        {
            return null;
        }

        if (finished)
        {
            return madeSeen ? null : "finished, but the table never said the plant was made (it said \"" + status + "\")";
        }

        return !madeSeen && mostPieces == 0 && piecesOffered ? "no piece stayed on the soil (the table said \"" + status + "\")" : null;
    }
}
