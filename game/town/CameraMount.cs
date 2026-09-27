namespace MmoGame3d.Town;

using Godot;

/// <summary>
/// Where one of the town's security cameras looks from, for the Town cameras app. A
/// camera within reach is the child of a Fixable (it breaks, and a broken one shows no
/// signal); one high on a roof or a lamp post stands alone and never breaks.
/// </summary>
public partial class CameraMount : Node3D
{
    public const string Group = "town_cameras";

    // The zone-local point it watches.
    [Export]
    public Vector3 Target { get; set; }

    public bool Working
    {
        get
        {
            Fixable? fixable = GetParent() as Fixable;
            return fixable == null || !fixable.Broken;
        }
    }

    public override void _EnterTree()
    {
        AddToGroup(Group);
    }
}
