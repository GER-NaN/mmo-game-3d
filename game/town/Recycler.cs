namespace MmoGame3d.Town;

using MmoGame3d.Interact;

/// <summary>
/// The recycling machine: put something in, get pocket change (see Recycling). What goes
/// in is decided on the server (ServerRecycling).
/// </summary>
public partial class Recycler : Interactable
{
    public override string Prompt
    {
        get { return "Use the recycler"; }
    }
}
