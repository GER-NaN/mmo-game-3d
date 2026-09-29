namespace MmoGame3d.Bots;

using System.Collections.Concurrent;
using Godot;

/// <summary>
/// Hears everything the engine logs and keeps the errors, warnings left out. The engine
/// may log from any thread, so errors wait in a queue for the watcher's next look.
/// </summary>
public partial class BotErrorLogger : Godot.Logger
{
    // errorType is Godot's ErrorType: 0 error, 1 warning, 2 script, 3 shader.
    private const int Warning = 1;

    public ConcurrentQueue<string> Errors { get; } = new ConcurrentQueue<string>();

    public override void _LogMessage(string message, bool error)
    {
        if (error)
        {
            Errors.Enqueue(message.Trim());
        }
    }

    public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType != Warning)
        {
            Errors.Enqueue((rationale.Length > 0 ? rationale : code) + " (" + file + ":" + line + ")");
        }
    }
}
