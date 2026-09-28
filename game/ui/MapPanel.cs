namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Rules.Maps;

/// <summary>
/// The map of the zone the player is in: a picture of the zone from straight above,
/// taken once when the map opens, with the cells the player has not found yet covered.
/// The picture is the zone itself, so the map never needs drawing by hand.
/// </summary>
public partial class MapPanel : Control
{
    private const float PictureHeight = 480f;
    private const float CameraHeight = 80f;
    private const float MarkerSize = 9f;

    // Placeholders, like every look.
    private static readonly Color Unfound = new Color(0.07f, 0.08f, 0.1f);
    private static readonly Color MarkerColor = new Color(1f, 0.85f, 0.2f);

    private Discovery _discovery = null!;
    private Control _fog = null!;

    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

    public void Open(string title, Vector2 mapSize, byte[]? cells)
    {
        _discovery = new Discovery(mapSize.X, mapSize.Y);
        GetNode<Label>("%Title").Text = title;
        GetNode<Control>("%Frame").CustomMinimumSize = new Vector2(PictureHeight * mapSize.X / mapSize.Y, PictureHeight);

        // Straight down with north (-Z) at the top, so the picture's x and y are the
        // zone's x and z. Its own light, so the map reads the same by night.
        Camera3D camera = GetNode<Camera3D>("%Camera");
        camera.Size = mapSize.Y;
        camera.Position = new Vector3(0f, CameraHeight, 0f);
        camera.RotationDegrees = new Vector3(-90f, 0f, 0f);
        camera.Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = Unfound,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.9f,
        };

        _fog = GetNode<Control>("%Fog");
        _fog.Draw += DrawFog;

        if (cells != null)
        {
            ShowCells(cells);
        }
    }

    public void ShowCells(byte[] cells)
    {
        _discovery.Load(cells);
        _fog.QueueRedraw();
    }

    // The marker follows the player while the map is open.
    public override void _Process(double delta)
    {
        _fog.QueueRedraw();
    }

    private void DrawFog()
    {
        Vector2 scale = _fog.Size / new Vector2(_discovery.Width, _discovery.Depth);
        Vector2 cell = scale * Discovery.CellSize;

        for (int row = 0; row < _discovery.Rows; row++)
        {
            for (int column = 0; column < _discovery.Columns; column++)
            {
                if (!_discovery.IsDiscovered(column, row))
                {
                    _fog.DrawRect(new Rect2(new Vector2(column, row) * cell, cell), Unfound);
                }
            }
        }

        Node3D? self = GetTree().GetFirstNodeInGroup(Players.Player.LocalGroup) as Node3D;

        if (self == null)
        {
            return;
        }

        Vector2 at = (new Vector2(self.GlobalPosition.X, self.GlobalPosition.Z) + new Vector2(_discovery.Width, _discovery.Depth) / 2f) * scale;
        Vector3 facing3 = -self.GlobalBasis.Z;
        Vector2 facing = new Vector2(facing3.X, facing3.Z).Normalized();
        Vector2 side = new Vector2(-facing.Y, facing.X);

        _fog.DrawColoredPolygon(
            new Vector2[]
            {
                at + (facing * MarkerSize),
                at - (facing * MarkerSize * 0.6f) + (side * MarkerSize * 0.7f),
                at - (facing * MarkerSize * 0.6f) - (side * MarkerSize * 0.7f),
            },
            MarkerColor);
    }
}
