namespace MmoGame3d.Diagnostics;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// The server's logs and traces, through OpenTelemetry. Whoever writes a record only
/// puts it on a bounded queue in memory; a batch processor thread per signal takes the
/// queue about once a second and writes it out. So no file or network I/O happens on
/// the game thread. When a queue is full, new records are dropped and counted rather
/// than making the game thread wait: the tick matters more than a complete log.
///
/// The batches go to a JSON lines file, and to a viewer by OTLP when one is given
/// (Grafana, see docs/engineering/diagnostics.md). The file is the complete record: it
/// alone counts its drops. The viewer's processor cannot sit behind the cap, since the
/// SDK only hands the service name to a processor it holds itself; it drops silently
/// when its queue is full, and never blocks either.
/// </summary>
public sealed class Telemetry : IDisposable
{
    // Every ActivitySource named MmoGame3d.* is traced: MmoGame3d.Server,
    // MmoGame3d.Data and whatever comes next.
    public const string SourcePrefix = "MmoGame3d";

    // Room for bursts: a hundred players sending a walk twenty times a second, with every
    // packet logged, is thousands of records a second. Placeholder sizes.
    public const int DefaultQueueSize = 65536;
    private const int ExportDelayMilliseconds = 1000;
    private const int ExportTimeoutMilliseconds = 30000;

    private readonly JsonLinesFile _file;
    private readonly BoundedProcessor<LogRecord> _logs;
    private readonly BoundedProcessor<Activity> _spans;
    private readonly TracerProvider _tracerProvider;
    private readonly ILoggerFactory _loggerFactory;

    // viewer: the OTLP/HTTP base address (http://localhost:4318), or null for none.
    public Telemetry(string service, string version, string filePath, int queueSize = DefaultQueueSize, string? viewer = null)
    {
        int batchSize = queueSize / 8;

        _file = new JsonLinesFile(filePath);
        JsonLinesLogExporter logExporter = new JsonLinesLogExporter(_file, service);
        JsonLinesSpanExporter spanExporter = new JsonLinesSpanExporter(_file, service);
        _logs = new BoundedProcessor<LogRecord>(new BatchLogRecordExportProcessor(logExporter, queueSize, ExportDelayMilliseconds, ExportTimeoutMilliseconds, batchSize), queueSize - batchSize);
        _spans = new BoundedProcessor<Activity>(new BatchActivityExportProcessor(spanExporter, queueSize, ExportDelayMilliseconds, ExportTimeoutMilliseconds, batchSize), queueSize - batchSize);
        logExporter.Exported = _logs.Exported;
        spanExporter.Exported = _spans.Exported;

        ResourceBuilder resource = ResourceBuilder.CreateDefault().AddService(service, serviceVersion: version);

        TracerProviderBuilder tracing = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resource)
            .AddSource(SourcePrefix + ".*")
            .SetSampler(new AlwaysOnSampler())
            .AddProcessor(_spans);

        if (viewer != null)
        {
            tracing.AddOtlpExporter(options =>
            {
                ToViewer(options, viewer, "/v1/traces");
                options.BatchExportProcessorOptions.MaxQueueSize = queueSize;
                options.BatchExportProcessorOptions.MaxExportBatchSize = batchSize;
                options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = ExportDelayMilliseconds;
            });
        }

        _tracerProvider = tracing.Build()!;

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resource);
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.AddProcessor(_logs);

                if (viewer != null)
                {
                    options.AddOtlpExporter((exporter, processor) =>
                    {
                        ToViewer(exporter, viewer, "/v1/logs");
                        processor.BatchExportProcessorOptions.MaxQueueSize = queueSize;
                        processor.BatchExportProcessorOptions.MaxExportBatchSize = batchSize;
                        processor.BatchExportProcessorOptions.ScheduledDelayMilliseconds = ExportDelayMilliseconds;
                    });
                }
            });
        });
    }

    public string FilePath
    {
        get { return _file.FilePath; }
    }

    public long LogsDropped
    {
        get { return _logs.Dropped; }
    }

    public long SpansDropped
    {
        get { return _spans.Dropped; }
    }

    public ILogger Logger(string category)
    {
        return _loggerFactory.CreateLogger(category);
    }

    // A base address set in code gets no signal path added, unlike one from the
    // environment, so the path is spelled out.
    private static void ToViewer(OtlpExporterOptions options, string viewer, string path)
    {
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Endpoint = new Uri(viewer.TrimEnd('/') + path);
    }

    // Flushes what is queued: disposing the providers exports the last batches.
    public void Dispose()
    {
        _loggerFactory.Dispose();
        _tracerProvider.Dispose();
        _file.Dispose();
    }
}

// Caps the records in flight (queued or being written) and counts what it drops. The
// batch processors count their own drops but keep the count internal, so the cap sits
// in front of them, below their queue size, and they never drop anything themselves.
internal sealed class BoundedProcessor<T> : BaseProcessor<T>
    where T : class
{
    private readonly BaseProcessor<T> _inner;
    private readonly long _cap;
    private long _inFlight;
    private long _dropped;

    public BoundedProcessor(BaseProcessor<T> inner, long cap)
    {
        _inner = inner;
        _cap = cap;
    }

    public long Dropped
    {
        get { return Interlocked.Read(ref _dropped); }
    }

    // Called by the exporter once a batch is written.
    public void Exported(int count)
    {
        Interlocked.Add(ref _inFlight, -count);
    }

    public override void OnEnd(T data)
    {
        if (Interlocked.Increment(ref _inFlight) > _cap)
        {
            Interlocked.Decrement(ref _inFlight);
            Interlocked.Increment(ref _dropped);
            return;
        }

        _inner.OnEnd(data);
    }

    protected override bool OnForceFlush(int timeoutMilliseconds)
    {
        return _inner.ForceFlush(timeoutMilliseconds);
    }

    protected override bool OnShutdown(int timeoutMilliseconds)
    {
        return _inner.Shutdown(timeoutMilliseconds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
