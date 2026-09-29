namespace MmoGame3d.Zones;

using Godot;

/// <summary>
/// A light that is on while the lights are on (DayNight.LightsOn: dark, and the town has
/// power): a street lamp, later a window's glow. It reads the state when it enters the
/// tree, so a zone that loads at night is lit at once, and follows it from then on. While
/// on it plays its hum, close up, if it has one.
/// </summary>
public partial class NightLight : Node3D
{
    // The sound catalog's name for the hum while lit ("fx.lamp"), or empty for none.
    [Export]
    public string Hum { get; set; } = "";

    private DayNight? _dayNight;

    public override void _EnterTree()
    {
        _dayNight = GetTree().GetFirstNodeInGroup(DayNight.Group) as DayNight;

        if (_dayNight == null)
        {
            Visible = false;
            return;
        }

        _dayNight.LightsChanged += Apply;
        Apply(_dayNight.LightsOn);
    }

    public override void _ExitTree()
    {
        if (_dayNight != null)
        {
            _dayNight.LightsChanged -= Apply;
            _dayNight = null;
        }
    }

    private void Apply(bool on)
    {
        Visible = on;
        AudioStreamPlayer3D? hum = GetNodeOrNull<AudioStreamPlayer3D>("Hum");

        if (on && hum == null && Hum.Length > 0)
        {
            AudioStreamPlayer3D? player = Audio.AudioDirector.Current?.Attach(Hum, this);

            if (player != null)
            {
                player.Name = "Hum";
            }
        }
        else if (!on && hum != null)
        {
            hum.QueueFree();
        }
    }
}
