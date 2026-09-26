namespace MmoGame3d.Client;

using Godot;

/// <summary>
/// What the player set in the menus, kept between launches in user://settings.cfg.
/// Shared by every profile on the machine, except the name, which is per profile.
/// </summary>
public class ClientSettings
{
    private const string Path = "user://settings.cfg";

    private readonly ConfigFile _file = new ConfigFile();

    public bool Fullscreen { get; set; }
    public string Address { get; set; } = "127.0.0.1";

    public static ClientSettings Load()
    {
        ClientSettings settings = new ClientSettings();

        // A missing file is a first launch, not an error.
        if (settings._file.Load(Path) == Error.Ok)
        {
            settings.Fullscreen = (bool)settings._file.GetValue("display", "fullscreen", false);
            settings.Address = (string)settings._file.GetValue("network", "address", "127.0.0.1");
        }

        return settings;
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
        _file.Save(Path);
    }

    public void Apply()
    {
        DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }
}
