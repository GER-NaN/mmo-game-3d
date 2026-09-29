namespace MmoGame3d.Rules.Town;

using System;
using System.Collections.Generic;

/// <summary>
/// Where a new tag goes on the subway wall: the spot nearest the one wanted (in front of
/// the player) where it overlaps no tag already there. Overlap is checked on each tag's
/// box turned by its slant, so the check is a little generous on slanted tags.
/// </summary>
public static class TagPlacement
{
    // Metres between spots tried.
    private const float Step = 0.1f;

    // Clear room kept round every tag.
    private const float Gap = 0.04f;

    // A letter's width, as a share of the font size; the font is not known here.
    private const float LetterWidth = 0.6f;

    // The tag's width on the wall, before its slant, in metres.
    public static float Width(string name, int fontSize)
    {
        return ((Math.Max(1, name.Length) * fontSize * LetterWidth) + (2 * SubwayWall.Outline)) * SubwayWall.PixelSize;
    }

    public static float Height(int fontSize)
    {
        return (fontSize + (2 * SubwayWall.Outline)) * SubwayWall.PixelSize;
    }

    // Null when the tag fits nowhere on the wall.
    public static TagPlace? Find(IEnumerable<SubwayTag> onWall, string name, int fontSize, float angle, float wantX, float wantY)
    {
        Box box = Box.Round(0f, 0f, name, fontSize, angle);
        List<Box> taken = new List<Box>();

        foreach (SubwayTag tag in onWall)
        {
            if (tag.Place != null)
            {
                taken.Add(Box.Round(tag.Place.X, tag.Place.Y, tag.Name, tag.Place.Size, tag.Place.Angle));
            }
        }

        float minX = (-SubwayWall.Width / 2f) + box.HalfWidth;
        float maxX = (SubwayWall.Width / 2f) - box.HalfWidth;
        float minY = SubwayWall.Low + box.HalfHeight;
        float maxY = SubwayWall.High - box.HalfHeight;
        TagPlace? best = null;
        float bestDistance = float.MaxValue;

        for (float x = minX; x <= maxX + 0.0001f; x += Step)
        {
            for (float y = minY; y <= maxY + 0.0001f; y += Step)
            {
                float distance = ((x - wantX) * (x - wantX)) + ((y - wantY) * (y - wantY));

                if (distance >= bestDistance || Overlaps(taken, x, y, box))
                {
                    continue;
                }

                bestDistance = distance;
                best = new TagPlace { X = x, Y = y, Angle = angle, Size = fontSize };
            }
        }

        return best;
    }

    private static bool Overlaps(List<Box> taken, float x, float y, Box box)
    {
        foreach (Box other in taken)
        {
            if (Math.Abs(x - other.X) < box.HalfWidth + other.HalfWidth && Math.Abs(y - other.Y) < box.HalfHeight + other.HalfHeight)
            {
                return true;
            }
        }

        return false;
    }

    // The upright box round a tag turned by its slant, with the gap.
    private class Box
    {
        public float X;
        public float Y;
        public float HalfWidth;
        public float HalfHeight;

        public static Box Round(float x, float y, string name, int fontSize, float angle)
        {
            float width = Width(name, fontSize);
            float height = Height(fontSize);
            double radians = Math.Abs(angle) * Math.PI / 180.0;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Box
            {
                X = x,
                Y = y,
                HalfWidth = (((width * cos) + (height * sin)) / 2f) + Gap,
                HalfHeight = (((width * sin) + (height * cos)) / 2f) + Gap,
            };
        }
    }
}
