namespace MmoGame3d.Networking;

using Godot;

// The RPC nodes under Main, found once and handed to both sides together.
public class Networks
{
    public Networks(Node main)
    {
        Session = main.GetNode<Network>("Network");
        Party = main.GetNode<PartyNetwork>("PartyNetwork");
        Terminal = main.GetNode<TerminalNetwork>("TerminalNetwork");
    }

    public Network Session { get; }
    public PartyNetwork Party { get; }
    public TerminalNetwork Terminal { get; }
}
