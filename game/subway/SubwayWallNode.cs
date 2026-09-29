namespace MmoGame3d.Subway;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Rules.Town;

/// <summary>
/// The tiled wall in the subway where players spray their names (SubwayWall has the
/// rules). The newest tags are synced as one text, each with the place the server gave
/// it; each client paints them there.
/// </summary>
public partial class SubwayWallNode : Interactable
{
    public const string Group = "subway_walls";

    public override void _EnterTree()
    {
        AddToGroup(Group);
    }

    [Export]
    public string Tags { get; set; } = "";

    private string _painted = "\u0000";
    private readonly List<Label3D> _labels = new List<Label3D>();

    public override string Prompt
    {
        get { return "Spray your name on the wall"; }
    }

    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer() || Tags == _painted)
        {
            return;
        }

        _painted = Tags;

        foreach (Label3D label in _labels)
        {
            label.QueueFree();
        }

        _labels.Clear();

        foreach (SubwayTag tag in SubwayWall.Unpack(Tags))
        {
            Paint(tag);
        }
    }

    // The paintable face is centred on the node, facing +Z.
    private void Paint(SubwayTag tag)
    {
        if (tag.Place == null)
        {
            return;
        }

        Label3D label = new Label3D
        {
            Text = tag.Name,
            Modulate = new Color(tag.Paint),
            OutlineModulate = new Color(0.08f, 0.08f, 0.1f, 0.9f),
            OutlineSize = SubwayWall.Outline,
            FontSize = tag.Place.Size,
            PixelSize = SubwayWall.PixelSize,
            Position = new Vector3(tag.Place.X, tag.Place.Y, 0.2f),
            RotationDegrees = new Vector3(0f, 0f, tag.Place.Angle),
            DoubleSided = false,
        };
        AddChild(label);
        _labels.Add(label);
    }
}
