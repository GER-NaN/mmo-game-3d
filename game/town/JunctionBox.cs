namespace MmoGame3d.Town;

using Godot;
using MmoGame3d.Interact;

/// <summary>
/// The junction box that feeds Main Street's lamps: where the street light repair is
/// done. Its prompt follows the synced town state, so it reads right for everyone.
/// </summary>
public partial class JunctionBox : Interactable
{
    public const string Group = "junction_boxes";

    // Client only: true while this player has the job, so a marker bobs over the box,
    // seen through buildings.
    public static bool Marked { get; set; }

    private MeshInstance3D? _marker;
    private double _bob;

    public override void _EnterTree()
    {
        AddToGroup(Group);
    }

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        StandardMaterial3D material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1f, 0.85f, 0.3f),
            NoDepthTest = true,
        };
        _marker = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.35f, BottomRadius = 0f, Height = 0.7f, Material = material },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        AddChild(_marker);
    }

    public override void _Process(double delta)
    {
        if (_marker == null)
        {
            return;
        }

        _marker.Visible = Marked;

        if (Marked)
        {
            _bob += delta;
            _marker.Position = new Vector3(0f, 3.2f + (0.25f * Mathf.Sin((float)_bob * 3f)), 0f);
        }
    }

    public override string Prompt
    {
        get
        {
            TownState? state = GetTree().GetFirstNodeInGroup(TownState.Group) as TownState;
            return state != null && state.LightsWorking ? "Junction box (working)" : "Repair the junction box";
        }
    }
}
