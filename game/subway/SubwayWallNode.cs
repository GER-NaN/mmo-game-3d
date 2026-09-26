namespace MmoGame3d.Subway;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Rules.Town;

/// <summary>
/// The tiled wall in the subway where players spray their names (SubwayWall has the
/// rules). The newest tags are synced as one text; each client paints them on the wall,
/// every tag in the same spot and slant on every screen, worked out from its number.
/// </summary>
public partial class SubwayWallNode : Interactable
{
    // The paintable face, centred on the node, facing +Z. Placeholders.
    private const float Width = 13f;
    private const float Low = 0.7f;
    private const float High = 2.6f;

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

    private void Paint(SubwayTag tag)
    {
        Random spot = new Random((int)(tag.Id * 7919 % int.MaxValue));
        Label3D label = new Label3D
        {
            Text = tag.Name,
            Modulate = new Color(tag.Paint),
            OutlineModulate = new Color(0.08f, 0.08f, 0.1f, 0.9f),
            OutlineSize = 10,
            FontSize = 56 + spot.Next(40),
            PixelSize = 0.006f,
            Position = new Vector3(((float)spot.NextDouble() - 0.5f) * Width, Low + ((float)spot.NextDouble() * (High - Low)), 0.2f),
            RotationDegrees = new Vector3(0f, 0f, (float)((spot.NextDouble() - 0.5) * 24.0)),
            DoubleSided = false,
        };
        AddChild(label);
        _labels.Add(label);
    }
}
