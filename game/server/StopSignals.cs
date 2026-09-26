namespace MmoGame3d.Server;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;

/// <summary>
/// Turns a stop request into a normal quit on the game thread, so the last saves run.
/// Two ways in:
///
/// - A stop file, which scripts/server-stop.ps1 writes. This is the way on Windows:
///   Ctrl+C in the server's window also reaches Godot's console wrapper, which dies and
///   takes the server with it before any handler here can act.
/// - SIGINT and SIGTERM (a Docker stop, Ctrl+C on Linux), which reach the server itself.
///
/// The signal handler only sets a flag. It never touches the game: it runs on a thread
/// of its own, where the engine's error reporting cannot take a C# backtrace.
/// </summary>
public sealed class StopSignals : IDisposable
{
    // After a SIGTERM the process is ended a few seconds later whatever the handler
    // does, so the handler waits for the save for less than that.
    private static readonly TimeSpan SignalWait = TimeSpan.FromSeconds(4);

    private readonly PosixSignalRegistration _interrupt;
    private readonly PosixSignalRegistration _terminate;
    private readonly ManualResetEventSlim _stopped = new ManualResetEventSlim(false);
    private readonly string _stopFile;
    private volatile bool _signalled;

    public StopSignals(int port)
    {
        _stopFile = StopFilePath(port);

        // A file left from a server that never got to read it must not stop this one.
        File.Delete(_stopFile);

        _interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        _terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
    }

    // user://server-stop-7070, per port, so two servers on one machine stop one at a time.
    public static string StopFilePath(int port)
    {
        return ProjectSettings.GlobalizePath("user://server-stop-" + port);
    }

    // Asked by the game thread; checking for the file is cheap enough to do each frame.
    public bool StopRequested()
    {
        if (_signalled)
        {
            return true;
        }

        if (File.Exists(_stopFile))
        {
            File.Delete(_stopFile);
            return true;
        }

        return false;
    }

    // Called by the game thread once the last saves are written.
    public void MarkStopped()
    {
        _stopped.Set();
    }

    public void Dispose()
    {
        _interrupt.Dispose();
        _terminate.Dispose();
    }

    private void OnSignal(PosixSignalContext context)
    {
        _signalled = true;
        context.Cancel = true;
        _stopped.Wait(SignalWait);
    }
}
