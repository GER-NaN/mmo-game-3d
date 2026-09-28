namespace MmoGame3d.Taxis;

using Godot;

/// <summary>
/// A ride's cabin state, synced to its riders: how long until the car arrives. The
/// dashboard display shows it.
/// </summary>
public partial class TaxiState : Node
{
    [Export]
    public float SecondsLeft { get; set; }

    // The cabin's arrival sign.
    [Export]
    public Label3D? Display { get; set; }

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        Label3D? display = Display;

        if (display != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(SecondsLeft));
            display.Text = seconds > 0 ? "Arriving in " + (seconds / 60) + ":" + (seconds % 60).ToString("00") : "Arrived";
        }
    }
}
