namespace MmoGame3d.Taxis;

using System.Collections.Generic;
using Godot;

/// <summary>
/// What riders see out of the cabin's windows: trees and lamps going by on both sides at
/// the taxi's speed, wrapping round. The cabin stands still in its own instance; this is
/// only the look of driving, and only on clients.
/// </summary>
public partial class TaxiView : Node3D
{
    private const float Span = 60f;
    private const float Spacing = 7.5f;
    private const float Side = 5f;

    private static readonly string[] Scenery =
    {
        "res://game/props/city_builder_bits/TreeA.tscn",
        "res://game/props/city_builder_bits/Streetlight.tscn",
        "res://game/props/city_builder_bits/TreeC.tscn",
        "res://game/props/city_builder_bits/BushA.tscn",
    };

    private readonly List<Node3D> _passing = new List<Node3D>();

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            QueueFree();
            return;
        }

        int count = (int)(Span / Spacing);

        for (int i = 0; i < count; i++)
        {
            foreach (float side in new[] { -Side, Side })
            {
                PackedScene scene = GD.Load<PackedScene>(Scenery[(i + (side > 0 ? 1 : 0)) % Scenery.Length]);
                Node3D thing = scene.Instantiate<Node3D>();

                // Only a view: nothing out there should block or be hit.
                if (thing is CollisionObject3D body)
                {
                    body.CollisionLayer = 0;
                }

                thing.Position = new Vector3(side, 0f, (i * Spacing) - (Span / 2f));
                AddChild(thing);
                _passing.Add(thing);
            }
        }
    }

    // The cabin faces -Z, so the world outside goes by towards +Z.
    public override void _Process(double delta)
    {
        float step = RoboTaxi.Speed * (float)delta;

        foreach (Node3D thing in _passing)
        {
            Vector3 at = thing.Position;
            at.Z += step;

            if (at.Z > Span / 2f)
            {
                at.Z -= Span;
            }

            thing.Position = at;
        }
    }
}
