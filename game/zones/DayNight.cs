namespace MmoGame3d.Zones;

using Godot;
using MmoGame3d.Rules.Time;
using MmoGame3d.Town;

/// <summary>
/// Lights the world for the hour: the sun climbs from the east and sets in the west,
/// warm near the horizon; at night a dim, cool moon takes over and the sky darkens.
/// The server says the time now and then; between, this node counts on by itself.
/// Strengths and colours are placeholders until seen in the game.
/// </summary>
public partial class DayNight : Node
{
    public const string Group = "day_night";

    private const float MaxSunEnergy = 1.2f;
    private const float MoonEnergy = 0.18f;
    private const float NightSky = 0.08f;
    private const float LightsOnBelow = 0.2f;

    // The highest the sun climbs, from the horizon; 75 degrees.
    private const float MaxElevation = 1.31f;

    private static readonly Color WarmLight = new Color(1f, 0.62f, 0.38f);
    private static readonly Color MoonLight = new Color(0.6f, 0.7f, 1f);

    [Export]
    public DirectionalLight3D? Sun { get; set; }

    [Export]
    public DirectionalLight3D? Moon { get; set; }

    [Export]
    public WorldEnvironment? WorldEnvironment { get; set; }

    private DirectionalLight3D _sun = null!;
    private DirectionalLight3D _moon = null!;
    private Environment _environment = null!;
    private double _secondsOfDay = 12 * 3600;
    private bool _lightsOn;
    private bool _known;

    public override void _Ready()
    {
        _sun = Sun!;
        _moon = Moon!;
        _environment = WorldEnvironment!.Environment;
        AddToGroup(Group);
    }

    // The lights are on: dark, and the town has power. What lights up at night reads this
    // and follows LightsChanged (NightLight); nothing is pushed into it.
    public event System.Action<bool>? LightsChanged;

    public bool LightsOn
    {
        get { return _known && _lightsOn; }
    }

    // Dark enough that the street lamps are on: night music and night sounds.
    public bool IsNight
    {
        get { return LightsOn; }
    }

    // "14:05", or empty until the server has said the time.
    public string ClockText
    {
        get
        {
            if (!_known)
            {
                return "";
            }

            int minutes = (int)(_secondsOfDay / 60.0);
            return (minutes / 60).ToString("00") + ":" + (minutes % 60).ToString("00");
        }
    }

    public void SetTime(double secondsOfDay)
    {
        _secondsOfDay = secondsOfDay;
        _known = true;
    }

    public override void _Process(double delta)
    {
        if (!_known)
        {
            return;
        }

        _secondsOfDay = (_secondsOfDay + delta) % WorldClock.SecondsPerDay;
        double hour = _secondsOfDay / 3600.0;

        float height = (float)Daylight.SunHeight(hour);
        float strength = (float)Daylight.SunStrength(hour);
        float warmth = (float)Daylight.Warmth(hour);

        // Across the sky from east (+X) at dawn to west (-X) at dusk. A directional light
        // shines along its -Z, so it is turned to point from the sun down at the world.
        float azimuth = (float)((hour - 12.0) / 12.0 * Mathf.Pi);
        float elevation = Mathf.Asin(Mathf.Clamp(height, -1f, 1f)) * (MaxElevation / (Mathf.Pi / 2f));

        _sun.Rotation = new Vector3(-Mathf.Max(elevation, 0.05f), azimuth + (Mathf.Pi / 2f), 0f);
        _sun.LightEnergy = strength * MaxSunEnergy;
        _sun.LightColor = Colors.White.Lerp(WarmLight, warmth);
        _sun.Visible = strength > 0f;

        _moon.Rotation = new Vector3(-0.9f, azimuth - (Mathf.Pi / 2f), 0f);
        _moon.LightColor = MoonLight;
        _moon.LightEnergy = MoonEnergy * (1f - strength);

        // The lights come on at dusk, if the town's lights work: the repair job decides.
        TownState? town = GetTree().GetFirstNodeInGroup(TownState.Group) as TownState;
        bool lit = strength < LightsOnBelow && (town == null || town.LightsWorking);

        if (lit != _lightsOn)
        {
            _lightsOn = lit;
            LightsChanged?.Invoke(lit);
        }

        float sky = Mathf.Lerp(NightSky, 1f, strength);
        _environment.BackgroundEnergyMultiplier = sky;
        _environment.AmbientLightEnergy = sky;
    }
}
