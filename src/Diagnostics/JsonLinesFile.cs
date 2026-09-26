namespace MmoGame3d.Diagnostics;

using System.Collections;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

/// <summary>
/// The file the exporters write to: one JSON object per line, logs and spans mixed in
/// the order their batches arrive. Only the exporters' threads write here, never the
/// game thread. Logs and spans export on two threads, so a batch holds the lock while
/// it writes.
/// </summary>
public sealed class JsonLinesFile : IDisposable
{
    private static readonly JsonWriterOptions Options = new JsonWriterOptions
    {
        // Chat and names are shown as typed rather than as \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly object _lock = new object();
    private readonly FileStream _stream;
    private readonly Utf8JsonWriter _writer;
    private static readonly byte[] NewLine = { (byte)'\n' };

    public JsonLinesFile(string path)
    {
        string? folder = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        FilePath = path;
        _stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 1 << 16);
        _writer = new Utf8JsonWriter(_stream, Options);
    }

    public string FilePath { get; }

    // Writes a batch under the lock and flushes it, so a reader sees whole lines about
    // once per export (a second or so), never a record cut in half.
    public void WriteBatch<T>(IEnumerable<T> items, Action<Utf8JsonWriter, T> write)
    {
        lock (_lock)
        {
            foreach (T item in items)
            {
                write(_writer, item);
                _writer.Flush();
                _writer.Reset();
                _stream.Write(NewLine, 0, 1);
            }

            _stream.Flush();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer.Dispose();
            _stream.Dispose();
        }
    }

    public static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case long number:
                writer.WriteNumberValue(number);
                break;
            case uint number:
                writer.WriteNumberValue(number);
                break;
            case ulong number:
                writer.WriteNumberValue(number);
                break;
            case float number:
                WriteFloat(writer, number);
                break;
            case double number:
                WriteDouble(writer, number);
                break;
            case byte[] bytes:
                writer.WriteBase64StringValue(bytes);
                break;
            case IEnumerable list:
                writer.WriteStartArray();

                foreach (object? item in list)
                {
                    WriteValue(writer, item);
                }

                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    public static void WriteAttributes(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object?>>? attributes, string? skip = null)
    {
        writer.WriteStartObject("attributes");

        if (attributes != null)
        {
            foreach (KeyValuePair<string, object?> attribute in attributes)
            {
                if (attribute.Key == skip)
                {
                    continue;
                }

                writer.WritePropertyName(attribute.Key);
                WriteValue(writer, attribute.Value);
            }
        }

        writer.WriteEndObject();
    }

    // JSON has no NaN or infinity.
    private static void WriteFloat(Utf8JsonWriter writer, float number)
    {
        if (float.IsFinite(number))
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteStringValue(number.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void WriteDouble(Utf8JsonWriter writer, double number)
    {
        if (double.IsFinite(number))
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteStringValue(number.ToString(CultureInfo.InvariantCulture));
        }
    }
}
