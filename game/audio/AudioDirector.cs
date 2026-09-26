namespace MmoGame3d.Audio;

using System.Collections.Generic;
using Godot;

/// <summary>
/// Plays the game's sounds on a client, by the ids in the catalog (game/audio/sounds.json):
/// one-off sounds, flat (Play) or from a spot in the world (PlayAt); one music track and
/// one ambience bed at a time, faded across when they change. The server plays nothing.
/// A sound whose file is not on this machine (assets are copied by hand) is skipped.
/// Also clicks for every button: the terminal's own bleeps inside the terminal, the
/// warm ones elsewhere.
/// </summary>
public partial class AudioDirector : Node
{
    public const string Catalog = "res://game/audio/sounds.json";
    private const string Folder = "res://assets/audio/";
    private const float FadeSeconds = 1.5f;
    private const float Silent = -60f;

    private readonly Dictionary<string, Sound> _sounds = new Dictionary<string, Sound>();
    private readonly Dictionary<string, AudioStream?> _streams = new Dictionary<string, AudioStream?>();
    private readonly List<AudioStreamPlayer> _flat = new List<AudioStreamPlayer>();
    private readonly RandomNumberGenerator _random = new RandomNumberGenerator();
    private AudioStreamPlayer _music = null!;
    private AudioStreamPlayer _ambience = null!;
    private string _musicId = "";
    private string _ambienceId = "";

    // The one on this client, for the few places that play a sound without a path to it.
    public static AudioDirector? Current { get; private set; }

    public override void _Ready()
    {
        Current = this;
        Load();
        _music = new AudioStreamPlayer { Bus = "Music" };
        _ambience = new AudioStreamPlayer { Bus = "Ambience" };
        AddChild(_music);
        AddChild(_ambience);

        for (int i = 0; i < 8; i++)
        {
            AudioStreamPlayer player = new AudioStreamPlayer();
            AddChild(player);
            _flat.Add(player);
        }

        GetTree().NodeAdded += OnNodeAdded;
    }

    public override void _ExitTree()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    public void Play(string id)
    {
        AudioStream? stream = Stream(id);

        if (stream == null)
        {
            return;
        }

        foreach (AudioStreamPlayer player in _flat)
        {
            if (!player.Playing)
            {
                Sound sound = _sounds[id];
                player.Stream = stream;
                player.Bus = sound.Bus;
                player.VolumeDb = sound.Level;
                player.Play();
                return;
            }
        }
    }

    // From a spot in the world; the player frees itself when done.
    public void PlayAt(string id, Vector3 at)
    {
        AudioStream? stream = Stream(id);
        Node3D? world = GetTree().CurrentScene as Node3D ?? GetTree().Root.FindChild("World", true, false) as Node3D;

        if (stream == null || world == null)
        {
            return;
        }

        Sound sound = _sounds[id];
        AudioStreamPlayer3D player = new AudioStreamPlayer3D
        {
            Stream = stream,
            Bus = sound.Bus,
            VolumeDb = sound.Level,
            MaxDistance = sound.Range,
            UnitSize = sound.Range / 4f,
        };
        world.AddChild(player);
        player.GlobalPosition = at;
        player.Finished += player.QueueFree;
        player.Play();
    }

    // A looping sound that comes from a thing and moves with it (a drone's hum, a lamp's
    // buzz). The caller frees it when the thing stops making it. Null when the sound is
    // not on this machine.
    public AudioStreamPlayer3D? Attach(string id, Node3D thing)
    {
        AudioStream? stream = Stream(id);

        if (stream == null)
        {
            return null;
        }

        Sound sound = _sounds[id];
        AudioStreamPlayer3D player = new AudioStreamPlayer3D
        {
            Stream = stream,
            Bus = sound.Bus,
            VolumeDb = sound.Level,
            MaxDistance = sound.Range,
            UnitSize = sound.Range / 4f,
            Autoplay = true,
        };
        thing.AddChild(player);
        return player;
    }

    // The music now: the same id carries on; another fades across; "" fades out.
    public void Music(string id)
    {
        Swap(_music, ref _musicId, id);
    }

    public void Ambience(string id)
    {
        Swap(_ambience, ref _ambienceId, id);
    }

    private void Swap(AudioStreamPlayer player, ref string current, string id)
    {
        if (id == current)
        {
            return;
        }

        current = id;
        AudioStream? stream = id.Length > 0 ? Stream(id) : null;
        GD.Print("Sound: " + (player == _music ? "music " : "ambience ") + (id.Length > 0 ? id : "off") + (id.Length > 0 && stream == null ? " (missing)" : ""));
        float level = stream != null ? _sounds[id].Level : Silent;
        Tween tween = player.CreateTween();

        if (player.Playing)
        {
            tween.TweenProperty(player, "volume_db", Silent, FadeSeconds / 2f);
        }

        tween.TweenCallback(Callable.From(() =>
        {
            player.Stop();

            if (stream != null)
            {
                player.Stream = stream;
                player.VolumeDb = Silent;
                player.Play();
            }
        }));

        if (stream != null)
        {
            tween.TweenProperty(player, "volume_db", level, FadeSeconds);
        }
    }

    // Every button clicks: the terminal's bleep inside the terminal, the warm click
    // elsewhere. Known when pressed, since a button may move.
    private void OnNodeAdded(Node node)
    {
        BaseButton? button = node as BaseButton;

        if (button != null)
        {
            button.Pressed += () => Play(InTerminal(button) ? "term.click" : "ui.click");
        }
    }

    private static bool InTerminal(Node node)
    {
        for (Node? at = node; at != null; at = at.GetParent())
        {
            if (at is Ui.TerminalScreen)
            {
                return true;
            }
        }

        return false;
    }

    // One of the id's files, picked at random, loaded once; null when not on this machine.
    private AudioStream? Stream(string id)
    {
        Sound? sound;

        if (!_sounds.TryGetValue(id, out sound) || sound.Files.Count == 0)
        {
            return null;
        }

        string file = sound.Files[_random.RandiRange(0, sound.Files.Count - 1)];
        AudioStream? stream;

        if (_streams.TryGetValue(file, out stream))
        {
            return stream;
        }

        string path = Folder + file;
        stream = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;

        if (stream == null)
        {
            GD.Print("Sound not on this machine: " + path + " (see tools/audio-subset)");
        }
        else if (sound.Loop)
        {
            Loop(stream);
        }

        _streams[file] = stream;
        return stream;
    }

    // Loops are set here, not in each file's import settings, so the catalog says it.
    private static void Loop(AudioStream stream)
    {
        AudioStreamWav? wav = stream as AudioStreamWav;

        if (wav != null)
        {
            int bytes = wav.Format == AudioStreamWav.FormatEnum.Format8Bits ? 1 : 2;
            int frames = wav.Data.Length / (bytes * (wav.Stereo ? 2 : 1));
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            wav.LoopBegin = 0;
            wav.LoopEnd = frames;
            return;
        }

        AudioStreamOggVorbis? ogg = stream as AudioStreamOggVorbis;

        if (ogg != null)
        {
            ogg.Loop = true;
        }
    }

    private void Load()
    {
        using FileAccess? file = FileAccess.Open(Catalog, FileAccess.ModeFlags.Read);

        if (file == null)
        {
            GD.PrintErr("No sound catalog at " + Catalog);
            return;
        }

        Godot.Collections.Dictionary root = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Godot.Collections.Dictionary sounds = root["sounds"].AsGodotDictionary();

        foreach (KeyValuePair<Variant, Variant> entry in sounds)
        {
            Godot.Collections.Dictionary fields = entry.Value.AsGodotDictionary();
            Sound sound = new Sound
            {
                Bus = fields.ContainsKey("bus") ? (string)fields["bus"] : "Master",
                Level = fields.ContainsKey("level") ? (float)fields["level"] : 0f,
                Loop = fields.ContainsKey("loop") && (bool)fields["loop"],
                Range = fields.ContainsKey("range") ? (float)fields["range"] : 20f,
            };

            foreach (Variant path in fields["files"].AsGodotArray())
            {
                sound.Files.Add((string)path);
            }

            _sounds[(string)entry.Key] = sound;
        }
    }

    private class Sound
    {
        public List<string> Files { get; } = new List<string>();
        public string Bus { get; set; } = "Master";
        public float Level { get; set; }
        public bool Loop { get; set; }
        public float Range { get; set; } = 20f;
    }
}
