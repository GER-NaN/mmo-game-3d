namespace MmoGame3d.Server;

using Godot;
using Microsoft.Extensions.Logging;
using MmoGame3d.Diagnostics;

/// <summary>
/// Everything Godot prints, ours (GD.Print) and the engine's (errors, warnings), copied
/// into the log. The engine may call this from any thread; the ILogger is safe for that.
/// Nothing here may print, or it would log itself.
/// </summary>
public partial class EngineLog : Godot.Logger
{
    public ILogger? Target { get; set; }

    public override void _LogMessage(string message, bool error)
    {
        Target?.Write(error ? LogLevel.Error : LogLevel.Information, new Fields(message.TrimEnd('\n'), 0));
    }

    public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (Target == null)
        {
            return;
        }

        // errorType is Godot's ErrorType: 0 error, 1 warning, 2 script, 3 shader.
        Fields fields = new Fields(rationale.Length > 0 ? rationale : code, 5)
            .With("code.function", function)
            .With("code.filepath", file)
            .With("code.lineno", line)
            .With("error.code", code)
            .With("error.type", errorType);
        Target.Write(errorType == 1 ? LogLevel.Warning : LogLevel.Error, fields);
    }
}
