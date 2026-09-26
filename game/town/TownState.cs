namespace MmoGame3d.Town;

using Godot;

/// <summary>
/// What everyone in town sees of its shared state: whether the street lights work. It
/// sits in the town scene on both sides and is synced from the server; the rules and
/// the saving are the server's (ServerTown).
/// </summary>
public partial class TownState : Node
{
    public const string Group = "town_state";

    [Export]
    public bool LightsWorking { get; set; }

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    public override void _Ready()
    {
        AddToGroup(Group);
    }
}
