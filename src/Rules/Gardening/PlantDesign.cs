namespace MmoGame3d.Rules.Gardening;

using System.Globalization;
using System.Text;

/// <summary>
/// A player's house plant as a design: a pot and up to five pieces. The client builds
/// it; the server checks it before it becomes a plant in the world, and the design is
/// all that is stored: anyone can rebuild the plant from it. Text form:
/// "pot;piece,x,z,yaw,tilt,scale;piece,...".
/// </summary>
public class PlantDesign
{
    public const int MaxPieces = 5;
    public const int MaxNameLength = 32;

    public string Pot { get; set; } = "pot_A_medium";
    public List<PlantPiece> Pieces { get; } = new List<PlantPiece>();

    public string Format()
    {
        StringBuilder text = new StringBuilder(Pot);

        foreach (PlantPiece piece in Pieces)
        {
            text.Append(';').Append(piece.Id);

            foreach (float value in new[] { piece.X, piece.Z, piece.Yaw, piece.Tilt, piece.Scale })
            {
                text.Append(',').Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        return text.ToString();
    }

    // Null when the text is not a valid design: an unknown pot or piece, none or too many
    // pieces, a piece off the soil, a lean or a size out of range.
    public static PlantDesign? Parse(string text)
    {
        string[] parts = (text ?? "").Split(';');

        if (parts.Length < 2 || parts.Length > MaxPieces + 1 || !PlantParts.IsPot(parts[0]))
        {
            return null;
        }

        PlantDesign design = new PlantDesign { Pot = parts[0] };

        for (int i = 1; i < parts.Length; i++)
        {
            string[] fields = parts[i].Split(',');
            float[] values = new float[5];

            if (fields.Length != 6 || !PlantParts.IsPiece(fields[0]))
            {
                return null;
            }

            for (int v = 0; v < 5; v++)
            {
                if (!float.TryParse(fields[v + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out values[v]) || !float.IsFinite(values[v]))
                {
                    return null;
                }
            }

            PlantPiece piece = new PlantPiece { Id = fields[0], X = values[0], Z = values[1], Yaw = values[2], Tilt = values[3], Scale = values[4] };

            if ((piece.X * piece.X) + (piece.Z * piece.Z) > 1.0001f
                || piece.Tilt < -PlantPiece.MaxTiltRadians || piece.Tilt > PlantPiece.MaxTiltRadians
                || piece.Scale < PlantPiece.MinScale || piece.Scale > PlantPiece.MaxScale)
            {
                return null;
            }

            design.Pieces.Add(piece);
        }

        return design;
    }
}
