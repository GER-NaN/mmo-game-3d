namespace MmoGame3d.Diagnostics;

using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Logs;

/// <summary>
/// Writes OpenTelemetry log records to the JSON lines file. The field names follow the
/// OpenTelemetry data model, so a viewer added later reads the same fields.
/// </summary>
public sealed class JsonLinesLogExporter : BaseExporter<LogRecord>
{
    private readonly JsonLinesFile _file;
    private readonly string _service;

    public JsonLinesLogExporter(JsonLinesFile file, string service)
    {
        _file = file;
        _service = service;
    }

    // Told how many records a batch held once it is written.
    public Action<int>? Exported { get; set; }

    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        _file.WriteBatch(Items(batch), Write);
        Exported?.Invoke((int)batch.Count);
        return ExportResult.Success;
    }

    private static IEnumerable<LogRecord> Items(Batch<LogRecord> batch)
    {
        foreach (LogRecord record in batch)
        {
            yield return record;
        }
    }

    private void Write(Utf8JsonWriter writer, LogRecord record)
    {
        writer.WriteStartObject();
        writer.WriteString("ts", record.Timestamp);
        writer.WriteString("type", "log");
        writer.WriteString("service", _service);
        writer.WriteString("level", record.LogLevel.ToString());
        writer.WriteString("logger", record.CategoryName);
        writer.WriteString("message", record.Body ?? record.FormattedMessage);

        if (record.TraceId != default)
        {
            writer.WriteString("trace_id", record.TraceId.ToHexString());
            writer.WriteString("span_id", record.SpanId.ToHexString());
        }

        JsonLinesFile.WriteAttributes(writer, record.Attributes, Fields.OriginalFormat);

        if (record.Exception != null)
        {
            writer.WriteString("exception", record.Exception.ToString());
        }

        writer.WriteEndObject();
    }
}
