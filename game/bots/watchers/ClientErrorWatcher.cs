namespace MmoGame3d.Bots;

using Godot;

/// <summary>
/// An error in the client's log (bots.md F1d): a C# exception, an engine error, a
/// printed error, as BotErrorLogger hears them. Each is a finding, except the few known
/// and harmless ones.
/// </summary>
public class ClientErrorWatcher : BotWatcher
{
    // Known, understood and harmless: left out so they do not bury the rest.
    private static readonly string[] Known =
    {
        // From the Terrain3D add-on, not our code.
        "instance_reset_physics_interpolation() is deprecated",
    };

    private readonly BotErrorLogger _logger = new BotErrorLogger();

    public ClientErrorWatcher()
        : base("client-error", false)
    {
        OS.AddLogger(_logger);
    }

    public override string? Look(BotBody body, BotStep? step, double delta)
    {
        string? error;

        while (_logger.Errors.TryDequeue(out error))
        {
            if (!IsKnown(error))
            {
                return error;
            }
        }

        return null;
    }

    private static bool IsKnown(string error)
    {
        foreach (string known in Known)
        {
            if (error.Contains(known))
            {
                return true;
            }
        }

        return false;
    }
}
