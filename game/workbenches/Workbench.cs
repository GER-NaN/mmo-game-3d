namespace MmoGame3d.Workbenches;

using MmoGame3d.Interact;

/// <summary>
/// A place to work on your things: here, take the battery out of a phone and put another
/// in. It stands on a desk or a table in the scene; the node is only the spot to use.
/// </summary>
public partial class Workbench : Interactable
{
    public override string Prompt
    {
        get { return "Use workbench"; }
    }
}
