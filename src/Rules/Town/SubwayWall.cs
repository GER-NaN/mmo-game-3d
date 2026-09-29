namespace MmoGame3d.Rules.Town;

using System.Collections.Generic;
using System.Text;

/// <summary>
/// The subway wall under Old Town: an underground visitor book (world.md, What players
/// change). A player sprays their name on it once, and it stays for good; nobody can
/// paint over it. The wall shows the newest tags. The tags travel to clients as one
/// text, a tag a line.
/// </summary>
public static class SubwayWall
{
    public const string OldTown = "old-town";

    // How many tags the wall shows.
    public const int Shown = 40;

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
            text.Append(tag.Id).Append('\t').Append(name).Append('\t').Append(tag.Paint).Append('\n');
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

            if (parts.Length == 3 && long.TryParse(parts[0], out id) && uint.TryParse(parts[2], out paint))
            {
                tags.Add(new SubwayTag { Id = id, Name = parts[1], Paint = paint });
            }
        }

        return tags;
    }
}
