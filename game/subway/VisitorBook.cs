namespace MmoGame3d.Subway;

using MmoGame3d.Interact;

/// <summary>
/// The visitor book on its stand in the subway: every name ever sprayed on the wall, in
/// order, a page at a time (ServerSubway sends the pages).
/// </summary>
public partial class VisitorBook : Interactable
{
    public override string Prompt
    {
        get { return "Read the visitor book"; }
    }
}
