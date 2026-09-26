namespace MmoGame3d.Terminals;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Rules.Terminals;

/// <summary>
/// A fixed terminal standing in a zone: a door into the terminal world. Whether it
/// works and who is online at it are synced, so everyone sees the same; the rules are
/// the server's (ServerTerminals).
/// </summary>
public partial class Terminal : Interactable
{
    private static readonly Color ScreenOn = new Color(0.2f, 0.9f, 0.5f);
    private static readonly Color ScreenBusy = new Color(0.95f, 0.75f, 0.25f);
    private static readonly Color ScreenOff = new Color(0.08f, 0.08f, 0.08f);

    private StandardMaterial3D? _screen;

    [Export]
    public int TypeId { get; set; }

    // The hook for the world taking a terminal out (AI control, a power cut).
    [Export]
    public bool Enabled { get; set; } = true;

    // The name of whoever is online here, or empty.
    [Export]
    public string UsedBy { get; set; } = "";

    public TerminalType Type
    {
        get { return (TerminalType)TypeId; }
    }

    public override string Prompt
    {
        get
        {
            if (!Enabled)
            {
                return "Terminal (not working)";
            }

            if (UsedBy.Length > 0)
            {
                return "In use by " + UsedBy;
            }

            return "Go Online: " + Type + " terminal";
        }
    }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
        {
            _screen = new StandardMaterial3D { EmissionEnabled = true };
            GetNode<MeshInstance3D>("Screen").MaterialOverride = _screen;
        }
    }

    public override void _Process(double delta)
    {
        if (_screen == null)
        {
            return;
        }

        Color color = !Enabled ? ScreenOff : UsedBy.Length > 0 ? ScreenBusy : ScreenOn;
        _screen.AlbedoColor = color;
        _screen.Emission = color;
    }
}
