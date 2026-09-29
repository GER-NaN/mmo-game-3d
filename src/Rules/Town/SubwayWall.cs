namespace MmoGame3d.Rules.Town;

using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// The subway wall under Old Town: an underground visitor book (world.md, What players
/// change). A player sprays their name on it once, and it stays for good; nobody can
/// paint over it. The wall shows the newest tags, each where the server placed it
/// (TagPlacement). The tags travel to clients as one text, a tag a line.
/// </summary>
public static class SubwayWall
{
    public const string OldTown = "old-town";

    // How many tags the wall shows.
    public const int Shown = 40;

    // The paintable face, centred on the wall, in metres. Placeholders.
    public const float Width = 13f;
    public const float Low = 0.7f;
    public const float High = 2.6f;

    // A tag's font sizes, and how far it slants either way, in degrees. Placeholders.
    public const int SmallestSize = 56;
    public const int LargestSize = 96;
    public const float MostSlant = 12f;

    // Metres a font pixel is on the wall, and the dark outline round each letter, in
    // font pixels.
    public const float PixelSize = 0.006f;
    public const int Outline = 10;

    // Spray paint colours, picked at random when a tag is made. Placeholders.
    public static readonly uint[] Paints =
    {
        0xff4d6dff, 0x3ec1ffff, 0xffd23fff, 0x7cff6bff, 0xc77dffff, 0xff8c42ff, 0xf5f5f5ff,
    };

    public static string Pack(IEnumerable<SubwayTag> tags)
    {
        StringBuilder text = new StringBuilder();

        foreach (SubwayTag tag in tags)
        {
            // Names are checked when chosen, but a tab or a line break would break the text.
            string name = tag.Name.Replace("\t", " ").Replace("\n", " ");
            text.Append(tag.Id).Append('\t').Append(name).Append('\t').Append(tag.Paint);

            if (tag.Place != null)
            {
                text.Append('\t').Append(tag.Place.X.ToString(CultureInfo.InvariantCulture))
                    .Append('\t').Append(tag.Place.Y.ToString(CultureInfo.InvariantCulture))
                    .Append('\t').Append(tag.Place.Angle.ToString(CultureInfo.InvariantCulture))
                    .Append('\t').Append(tag.Place.Size.ToString(CultureInfo.InvariantCulture));
            }

            text.Append('\n');
        }

        return text.ToString();
    }

    public static List<SubwayTag> Unpack(string text)
    {
        List<SubwayTag> tags = new List<SubwayTag>();

        foreach (string line in text.Split('\n'))
        {
            string[] parts = line.Split('\t');
            long id;
            uint paint;

            if ((parts.Length == 3 || parts.Length == 7) && long.TryParse(parts[0], out id) && uint.TryParse(parts[2], out paint))
            {
                SubwayTag tag = new SubwayTag { Id = id, Name = parts[1], Paint = paint };
                float x;
                float y;
                float angle;
                int size;

                if (parts.Length == 7
                    && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    && float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                    && float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out angle)
                    && int.TryParse(parts[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out size))
                {
                    tag.Place = new TagPlace { X = x, Y = y, Angle = angle, Size = size };
                }

                tags.Add(tag);
            }
        }

        return tags;
    }
}
