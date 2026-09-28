namespace MmoGame3d.Diagnostics;
using OpenTelemetry;

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
