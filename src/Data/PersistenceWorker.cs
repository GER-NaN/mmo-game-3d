namespace MmoGame3d.Data;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Runs database work on its own thread, so a slow query never stalls a server frame.
/// Work goes in from the game thread; each result comes back as a callback queued for
/// the game thread, which runs them when it calls RunCompletions. So game state is only
/// ever touched on the game thread, without locks and without async.
/// </summary>
public sealed class PersistenceWorker : IDisposable
{
    // Each job is a span, a child of whatever was being traced when it was queued (the
    // login RPC, say), and so is its completion back on the game thread. Nothing listens
    // unless the server turned tracing on, and then StartActivity costs next to nothing.
    public static readonly ActivitySource Source = new ActivitySource("MmoGame3d.Data");

    private readonly BlockingCollection<Action> _jobs = new BlockingCollection<Action>();
    private readonly ConcurrentQueue<Action> _completions = new ConcurrentQueue<Action>();
    private readonly Thread _thread;

    public PersistenceWorker()
    {
        _thread = new Thread(Work) { IsBackground = true, Name = "Persistence" };
        _thread.Start();
    }

    // Jobs still queued or running, for shutdown and for tests.
    public int Pending
    {
        get { return _jobs.Count; }
    }

    // The caller's name names the span, so no call site has to.
    public void Enqueue<T>(Func<T> work, Action<T> done, Action<Exception> failed, [CallerMemberName] string caller = "")
    {
        ActivityContext parent = Activity.Current?.Context ?? default;
        long queuedAt = Stopwatch.GetTimestamp();

        _jobs.Add(() =>
        {
            ActivityContext job = default;

            using (Activity? span = Source.StartActivity("db " + caller, ActivityKind.Internal, parent))
            {
                span?.SetTag("db.queue_wait_ms", Stopwatch.GetElapsedTime(queuedAt).TotalMilliseconds);
                job = span?.Context ?? parent;

                try
                {
                    T result = work();
                    _completions.Enqueue(() => Complete(job, caller, () => done(result)));
                }
                catch (Exception e)
                {
                    span?.SetStatus(ActivityStatusCode.Error, e.Message);
                    _completions.Enqueue(() => Complete(job, caller, () => failed(e)));
                }
            }
        });
    }

    // Fire and forget, for saves: a failure still comes back, so it can be logged.
    public void Enqueue(Action work, Action<Exception> failed, [CallerMemberName] string caller = "")
    {
        Enqueue<bool>(() =>
        {
            work();
            return true;
        }, _ => { }, failed, caller);
    }

    public int RunCompletions()
    {
        int ran = 0;

        while (_completions.TryDequeue(out Action? completion))
        {
            completion();
            ran++;
        }

        return ran;
    }

    /// <summary>
    /// Stops taking work and waits up to the timeout for what is queued, so the saves of
    /// the players still online at shutdown are written. Returns false on a timeout.
    /// </summary>
    public bool Stop(TimeSpan timeout)
    {
        _jobs.CompleteAdding();
        return _thread.Join(timeout);
    }

    public void Dispose()
    {
        if (!_jobs.IsAddingCompleted)
        {
            _jobs.CompleteAdding();
        }

        _jobs.Dispose();
    }

    private static void Complete(ActivityContext job, string caller, Action completion)
    {
        using (Source.StartActivity("db done " + caller, ActivityKind.Internal, job))
        {
            completion();
        }
    }

    private void Work()
    {
        foreach (Action job in _jobs.GetConsumingEnumerable())
        {
            job();
        }
    }
}
