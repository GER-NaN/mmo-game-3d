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
        Shop = main.GetNode<ShopNetwork>("ShopNetwork");
        Items = main.GetNode<ItemNetwork>("ItemNetwork");
        Social = main.GetNode<SocialNetwork>("SocialNetwork");
        Progress = main.GetNode<ProgressNetwork>("ProgressNetwork");
    }

    public Network Session { get; }
    public PartyNetwork Party { get; }
    public TerminalNetwork Terminal { get; }
    public ShopNetwork Shop { get; }
    public ItemNetwork Items { get; }
    public SocialNetwork Social { get; }
    public ProgressNetwork Progress { get; }

    public void SetLog(IRpcLog log)
    {
        Session.Log = log;
        Party.Log = log;
        Terminal.Log = log;
        Shop.Log = log;
        Items.Log = log;
        Social.Log = log;
        Progress.Log = log;
    }
}
