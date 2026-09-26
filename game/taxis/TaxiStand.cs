namespace MmoGame3d.Taxis;

using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Town;

// Where a robo taxi is called. Each call is its own ride, with its own car and cabin.
// While the AI's rootkit is in the taxis the sign goes red and nobody can call one.
public partial class TaxiStand : Interactable
{
    private bool? _shownClean;

    public override string Prompt
    {
        get { return Clean ? "Call a robo taxi" : "Robo taxi: out of service (rootkit)"; }
    }

    private bool Clean
    {
        get
        {
            TownState? state = GetTree().GetFirstNodeInGroup(TownState.Group) as TownState;
            return state == null || state.TaxisClean;
        }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer() || _shownClean == Clean)
        {
            return;
        }

        _shownClean = Clean;
        Label3D label = GetNode<Label3D>("Label");
        label.Text = Clean ? "TAXI" : "ROOTKIT";
        MeshInstance3D sign = GetNode<MeshInstance3D>("Sign");
        StandardMaterial3D look = new StandardMaterial3D
        {
            AlbedoColor = Clean ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.15f, 0.12f),
            EmissionEnabled = true,
            Emission = Clean ? new Color(0.4f, 0.3f, 0.05f) : new Color(0.5f, 0.05f, 0.03f),
        };
        sign.MaterialOverride = look;
    }
}
