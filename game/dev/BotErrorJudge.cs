namespace MmoGame3d.Dev;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Judges the client itself: every error the engine or our code reports in the bot's
/// client (a C# exception, a failed node lookup, a script error) becomes a finding, with
/// a picture of the moment, where the bot was and what it was doing, and the error's
/// stack. The game logs such errors and plays on, so without this they pass unseen.
///
/// The engine may report from any thread, so errors are only queued there; the bot takes
/// them on its own frame. The same error is reported once per bot per RepeatAfter.
/// Warnings are left out.
/// </summary>
public sealed partial class BotErrorJudge : Godot.Logger
{
    private const double RepeatAfter = 300;
    private const int StackFrames = 12;

    private readonly ConcurrentQueue<Error> _queue = new ConcurrentQueue<Error>();
    private readonly Dictionary<string, double> _reportedAt = new Dictionary<string, double>();
    private string _profile = "";
    private double _clock;

    public void Start(string profile)
    {
        _profile = profile;
        OS.AddLogger(this);
    }

    public void Stop()
    {
        OS.RemoveLogger(this);
    }

    public override void _LogMessage(string message, bool error)
    {
        if (error)
        {
            _queue.Enqueue(new Error(message.TrimEnd('\n'), "", ""));
        }
    }

    public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        // errorType is Godot's ErrorType: 0 error, 1 warning, 2 script, 3 shader.
        if (errorType == 1)
        {
            return;
        }

        StringBuilder stack = new StringBuilder();

        foreach (ScriptBacktrace backtrace in scriptBacktraces)
        {
            for (int i = 0; i < backtrace.GetFrameCount() && i < StackFrames; i++)
            {
                stack.Append(backtrace.GetFrameFunction(i)).Append(" (").Append(backtrace.GetFrameFile(i)).Append(':').Append(backtrace.GetFrameLine(i)).Append(")\n");
            }
        }

        _queue.Enqueue(new Error(rationale.Length > 0 ? rationale : code, function + " (" + file + ":" + line + ")", stack.ToString()));
    }

    // On the bot's frame: each new error as a finding.
    public void Tick(BotBody body, double delta, string activity, BotStep? step)
    {
        _clock += delta;
        Player? me = body.Me;

        while (_queue.TryDequeue(out Error? error))
        {
            double last;

            if (me == null || (_reportedAt.TryGetValue(error.Message, out last) && _clock - last < RepeatAfter))
            {
                continue;
            }

            _reportedAt[error.Message] = _clock;
            Vector3 at = me.GlobalPosition;
            BotFindings.Write(me, _profile, "client-error", error.Message.Length > 200 ? error.Message.Substring(0, 200) : error.Message, new Dictionary<string, object?>
            {
                { "zone", body.ZoneId },
                { "position", new double[] { System.Math.Round(at.X, 2), System.Math.Round(at.Y, 2), System.Math.Round(at.Z, 2) } },
                { "activity", activity },
                { "step", step?.Name ?? "" },
                { "where", error.Where },
                { "stack", error.Stack },
            });
        }
    }

    private sealed class Error
    {
        public Error(string message, string where, string stack)
        {
            Message = message;
            Where = where;
            Stack = stack;
        }

        public string Message { get; }

        public string Where { get; }

        public string Stack { get; }
    }
}
