namespace MmoGame3d.Client;

using System;
using System.IO;
using Godot;

/// <summary>
/// Which player this client is. A profile is a folder under the user data folder with
/// the license key in it, so the same profile is the same player on every launch, and
/// several clients on one machine are different players. The profile "fresh" is a new
/// player on every launch and writes nothing.
/// </summary>
public class Profile
{
    public const string Fresh = "fresh";

    private const string LicenseFile = "license.txt";
    private const string LockFile = "in-use";

    // Held open while this client runs; the system lets it go when the process ends.
    private FileStream? _lock;

    // A fresh profile's key, made once, so leaving and playing again is the same player.
    private readonly Guid _freshKey = Guid.NewGuid();

    public Profile(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool IsFresh
    {
        get { return Name == Fresh; }
    }

    // Holds the profile for this client, so a second game window with the same profile can
    // tell it is taken. False when another client holds it. A fresh profile is never shared.
    public bool Claim()
    {
        if (IsFresh || _lock != null)
        {
            return true;
        }

        string folder = ProjectSettings.GlobalizePath("user://profiles/" + Name);
        Directory.CreateDirectory(folder);

        try
        {
            _lock = new FileStream(Path.Combine(folder, LockFile), FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    // The file is a bare secret: copy it and you have cloned the player. It is the
    // placeholder until real auth, when it becomes a login instead.
    public Guid LicenseKey()
    {
        if (IsFresh)
        {
            return _freshKey;
        }

        string folder = ProjectSettings.GlobalizePath("user://profiles/" + Name);
        string path = Path.Combine(folder, LicenseFile);

        if (File.Exists(path))
        {
            Guid existing;

            // Making a new key here would silently make a new player. Failing is honest.
            if (!Guid.TryParse(File.ReadAllText(path).Trim(), out existing))
            {
                throw new InvalidDataException("The license file does not hold a key: " + path);
            }

            return existing;
        }

        Guid key = Guid.NewGuid();
        Directory.CreateDirectory(folder);
        File.WriteAllText(path, key.ToString());
        return key;
    }
}
