namespace MmoGame3d.Client;

using System.Collections.Generic;
using Godot;

/// <summary>
/// What the player set in the menus, kept between launches in user://settings.cfg (or
/// the file --settings-file names). Shared by every profile on the machine, except the
/// name, which is per profile.
/// </summary>
public class ClientSettings
{
    public const string DefaultPath = "user://settings.cfg";

    public const float MinSensitivity = 0.25f;
    public const float MaxSensitivity = 3f;

    // The actions a player may put on other keys, in the order the settings list them.
    // Enter (chat) and Esc (menu) stay where they are: Esc is the way out of everything.
    public static readonly string[] Rebindable =
    {
        "move_forward", "move_back", "turn_left", "turn_right", "strafe_left", "strafe_right",
        "jump", "interact", "phone", "inventory", "map", "social", "skills", "emp",
    };

    private readonly ConfigFile _file = new ConfigFile();
    private string _path = DefaultPath;

    public bool Fullscreen { get; set; }
    public string Address { get; set; } = "127.0.0.1";

    // A multiplier on how far a right-drag turns the camera.
    public float MouseSensitivity { get; set; } = 1f;

    // How far behind the player the camera sits; the wheel changes it too.
    public float CameraDistance { get; set; } = 8f;

    // The audio buses a player can turn down, and how loud each is, 0 to 1.
    public static readonly string[] VolumeBuses = { "Master", "Music", "Ambience", "Effects", "Interface" };
    public Dictionary<string, float> Volumes { get; } = new Dictionary<string, float>
    {
        { "Master", 0.8f }, { "Music", 0.7f }, { "Ambience", 0.8f }, { "Effects", 0.8f }, { "Interface", 0.8f },
    };

    // Only the keys the player moved; the rest are the defaults in project.godot.
    private readonly Dictionary<string, Key> _keys = new Dictionary<string, Key>();

    public static ClientSettings Load(string path)
    {
        ClientSettings settings = new ClientSettings();
        settings._path = path;

        // A missing file is a first launch, not an error.
        if (settings._file.Load(path) == Error.Ok)
        {
            settings.Fullscreen = (bool)settings._file.GetValue("display", "fullscreen", false);
            settings.Address = (string)settings._file.GetValue("network", "address", "127.0.0.1");
            settings.MouseSensitivity = Mathf.Clamp((float)settings._file.GetValue("controls", "mouse_sensitivity", 1f), MinSensitivity, MaxSensitivity);
            settings.CameraDistance = (float)settings._file.GetValue("controls", "camera_distance", 8f);

            foreach (string bus in VolumeBuses)
            {
                settings.Volumes[bus] = Mathf.Clamp((float)settings._file.GetValue("audio", bus.ToLowerInvariant(), settings.Volumes[bus]), 0f, 1f);
            }

            foreach (string action in Rebindable)
            {
                long key = (long)settings._file.GetValue("keys", action, 0L);

                if (key != 0)
                {
                    settings._keys[action] = (Key)key;
                    SetKey(action, (Key)key);
                }
            }
        }

        return settings;
    }

    // The physical key: where the key sits, not its letter, so WASD stays in place on
    // other keyboard layouts.
    public static Key KeyOf(string action)
    {
        foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
        {
            InputEventKey? key = inputEvent as InputEventKey;

            if (key != null)
            {
                return key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            }
        }

        return Key.None;
    }

    // What the key says on this keyboard.
    public static string KeyName(string action)
    {
        Key key = KeyOf(action);

        if (key == Key.None)
        {
            return "-";
        }

        // A headless client has no keyboard layout to ask; the physical key's own
        // name is the US one.
        if (DisplayServer.GetName() == "headless")
        {
            return OS.GetKeycodeString(key);
        }

        return OS.GetKeycodeString(DisplayServer.KeyboardGetKeycodeFromPhysical(key));
    }

    // A key already used by another action swaps with this one's old key, so no action is
    // ever left without a key.
    public void Bind(string action, Key key)
    {
        Key old = KeyOf(action);

        foreach (string other in Rebindable)
        {
            if (other != action && KeyOf(other) == key)
            {
                SetKey(other, old);
                _keys[other] = old;
            }
        }

        SetKey(action, key);
        _keys[action] = key;
    }

    public void ResetKeys()
    {
        InputMap.LoadFromProjectSettings();
        _keys.Clear();
    }

    private static void SetKey(string action, Key key)
    {
        foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
        {
            if (inputEvent is InputEventKey)
            {
                InputMap.ActionEraseEvent(action, inputEvent);
            }
        }

        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    public string NameFor(string profile)
    {
        return (string)_file.GetValue("names", profile, "");
    }

    public void SetNameFor(string profile, string name)
    {
        _file.SetValue("names", profile, name);
    }

    public void Save()
    {
        _file.SetValue("display", "fullscreen", Fullscreen);
        _file.SetValue("network", "address", Address);
        _file.SetValue("controls", "mouse_sensitivity", MouseSensitivity);
        _file.SetValue("controls", "camera_distance", CameraDistance);

        foreach (string bus in VolumeBuses)
        {
            _file.SetValue("audio", bus.ToLowerInvariant(), Volumes[bus]);
        }

        if (_file.HasSection("keys"))
        {
            _file.EraseSection("keys");
        }

        foreach (KeyValuePair<string, Key> binding in _keys)
        {
            _file.SetValue("keys", binding.Key, (long)binding.Value);
        }

        _file.Save(_path);
    }

    // Each bus at its volume; all the way down mutes it.
    public void ApplyVolumes()
    {
        foreach (string bus in VolumeBuses)
        {
            int index = AudioServer.GetBusIndex(bus);

            if (index >= 0)
            {
                AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(Volumes[bus], 0.0001f)));
                AudioServer.SetBusMute(index, Volumes[bus] <= 0.001f);
            }
        }
    }

    public void Apply()
    {
        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }
}
