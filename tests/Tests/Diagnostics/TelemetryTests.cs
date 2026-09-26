namespace MmoGame3d.Tests.Diagnostics;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MmoGame3d.Diagnostics;

public class TelemetryTests
{
    private static readonly ActivitySource Source = new ActivitySource("MmoGame3d.Tests");

    [Fact]
    public void ALogInsideASpanReachesTheFileWithItsAttributesAndTrace()
    {
        string path = Path.Combine(Path.GetTempPath(), "telemetry-" + Guid.NewGuid() + ".jsonl");
        string traceId;

        using (Telemetry telemetry = new Telemetry("test", "1", path))
        {
            ILogger logger = telemetry.Logger("Test");

            using (Activity? span = Source.StartActivity("Handle"))
            {
                traceId = span!.TraceId.ToHexString();
                logger.Write(LogLevel.Information, new Fields("packet in").With("net.peer", 42L).With("net.payload", new byte[] { 1, 2 }));
            }
        }

        List<JsonElement> lines = File.ReadAllLines(path).Select(line => JsonDocument.Parse(line).RootElement).ToList();
        JsonElement log = lines.Single(line => line.GetProperty("type").GetString() == "log");
        JsonElement spanLine = lines.Single(line => line.GetProperty("type").GetString() == "span");

        Assert.Equal("packet in", log.GetProperty("message").GetString());
        Assert.Equal(42, log.GetProperty("attributes").GetProperty("net.peer").GetInt64());
        Assert.Equal("AQI=", log.GetProperty("attributes").GetProperty("net.payload").GetString());
        Assert.Equal(traceId, log.GetProperty("trace_id").GetString());
        Assert.Equal("Handle", spanLine.GetProperty("name").GetString());
        File.Delete(path);
    }

    [Fact]
    public void AFullQueueDropsAndCountsInsteadOfWaiting()
    {
        string path = Path.Combine(Path.GetTempPath(), "telemetry-" + Guid.NewGuid() + ".jsonl");
        const int Written = 20000;
        long dropped;

        using (Telemetry telemetry = new Telemetry("test", "1", path, 64))
        {
            ILogger logger = telemetry.Logger("Test");

            for (int i = 0; i < Written; i++)
            {
                logger.Write(LogLevel.Debug, new Fields("burst").With("i", i));
            }

            dropped = telemetry.LogsDropped;
        }

        Assert.True(dropped > 0);
        Assert.Equal(Written, File.ReadAllLines(path).Length + dropped);
        File.Delete(path);
    }
}
