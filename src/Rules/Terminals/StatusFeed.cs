namespace MmoGame3d.Rules.Terminals;

using System.Collections.Generic;

/// <summary>
/// The terminal's status board: what is happening in the world right now, newest first
/// ("18:02  Two drones are up over Old Town"). Only the last few are kept, and only in
/// memory: it is news, not a record. The town log is the record.
/// </summary>
public class StatusFeed
{
    public const int Kept = 12;

    private readonly List<string> _lines = new List<string>();

    public IReadOnlyList<string> Lines
    {
        get { return _lines; }
    }

    // "hh:mm" is the world clock's time, from the caller.
    public void Post(string clock, string text)
    {
        _lines.Insert(0, clock + "  " + text);

        if (_lines.Count > Kept)
        {
            _lines.RemoveAt(_lines.Count - 1);
        }
    }
}
