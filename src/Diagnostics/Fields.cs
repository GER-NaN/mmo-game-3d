namespace MmoGame3d.Diagnostics;
/// <summary>
/// One log record's message and attributes, in the shape OpenTelemetry reads from an
/// ILogger state: a list of key/value pairs, with the message as "{OriginalFormat}",
/// which becomes the record's body. The message is a fixed text ("packet in"), never
/// built from the values, so writing a record formats nothing on the game thread; the
/// values are turned into text by the exporter, on its own thread.
/// </summary>
public sealed class Fields : IReadOnlyList<KeyValuePair<string, object?>>
{
    // The key the message is kept under: OpenTelemetry makes it the body, and the
    // exporter leaves it out of the attributes.
    public const string OriginalFormat = "{OriginalFormat}";

    private readonly List<KeyValuePair<string, object?>> _items;

    public Fields(string message, int capacity = 8)
    {
        Message = message;
        _items = new List<KeyValuePair<string, object?>>(capacity + 1);
    }

    public string Message { get; }

    public int Count
    {
        get { return _items.Count + 1; }
    }

    public KeyValuePair<string, object?> this[int index]
    {
        get { return index < _items.Count ? _items[index] : new KeyValuePair<string, object?>(OriginalFormat, Message); }
    }

    public Fields With(string key, object? value)
    {
        _items.Add(new KeyValuePair<string, object?>(key, value));
        return this;
    }

    public Fields WithAll(IReadOnlyList<KeyValuePair<string, object?>>? more)
    {
        if (more != null)
        {
            _items.AddRange(more);
        }

        return this;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override string ToString()
    {
        return Message;
    }
}
