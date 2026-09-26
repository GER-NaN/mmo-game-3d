namespace MmoGame3d.Diagnostics;

using System.Diagnostics;
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

/// <summary>
/// Writes finished spans to the JSON lines file, with their events.
/// </summary>
public sealed class JsonLinesSpanExporter : BaseExporter<Activity>
{
    private readonly JsonLinesFile _file;
    private readonly string _service;

    public JsonLinesSpanExporter(JsonLinesFile file, string service)
    {
        _file = file;
        _service = service;
    }

    // Told how many records a batch held once it is written.
    public Action<int>? Exported { get; set; }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        _file.WriteBatch(Items(batch), Write);
        Exported?.Invoke((int)batch.Count);
        return ExportResult.Success;
    }

    private static IEnumerable<Activity> Items(Batch<Activity> batch)
    {
        foreach (Activity span in batch)
        {
            yield return span;
        }
    }

    private void Write(Utf8JsonWriter writer, Activity span)
    {
        writer.WriteStartObject();
        writer.WriteString("ts", span.StartTimeUtc);
        writer.WriteString("type", "span");
        writer.WriteString("service", _service);
        writer.WriteString("name", span.DisplayName);
        writer.WriteString("kind", span.Kind.ToString());
        writer.WriteString("source", span.Source.Name);
        writer.WriteString("trace_id", span.TraceId.ToHexString());
        writer.WriteString("span_id", span.SpanId.ToHexString());

        if (span.ParentSpanId != default)
        {
            writer.WriteString("parent_id", span.ParentSpanId.ToHexString());
        }

        writer.WriteNumber("duration_ms", span.Duration.TotalMilliseconds);
        writer.WriteString("status", span.Status.ToString());

        if (span.StatusDescription != null)
        {
            writer.WriteString("status_message", span.StatusDescription);
        }

        JsonLinesFile.WriteAttributes(writer, span.TagObjects);
        writer.WriteStartArray("events");

        foreach (ActivityEvent spanEvent in span.Events)
        {
            writer.WriteStartObject();
            writer.WriteString("ts", spanEvent.Timestamp.UtcDateTime);
            writer.WriteString("name", spanEvent.Name);
            JsonLinesFile.WriteAttributes(writer, spanEvent.Tags);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
