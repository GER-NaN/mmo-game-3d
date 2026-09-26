namespace MmoGame3d.Gardening;

using MmoGame3d.Interact;

/// <summary>
/// The greenhouse's work table: using it starts the house plant mini game. The plant is
/// made on the client and checked on the server when it is finished (ServerGarden).
/// </summary>
public partial class PottingTable : Interactable
{
    public override string Prompt
    {
        get { return "Make a house plant"; }
    }
}
