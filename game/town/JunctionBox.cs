namespace MmoGame3d.Town;

using Godot;
using MmoGame3d.Interact;

/// <summary>
/// The junction box that feeds Main Street's lamps: where the street light repair is
/// done. Its prompt follows the synced town state, so it reads right for everyone.
/// </summary>
public partial class JunctionBox : Interactable
{
    public override string Prompt
    {
        get
        {
            TownState? state = GetTree().GetFirstNodeInGroup(TownState.Group) as TownState;
            return state != null && state.LightsWorking ? "Junction box (working)" : "Repair the junction box";
        }
    }
}
