namespace MmoGame3d.Taxis;

using MmoGame3d.Interact;

// Where a robo taxi is called. Each call is its own ride, with its own car and cabin.
public partial class TaxiStand : Interactable
{
    public override string Prompt
    {
        get { return "Call a robo taxi"; }
    }
}
