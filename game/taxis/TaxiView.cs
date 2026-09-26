namespace MmoGame3d.Taxis;

using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// What riders see out of the cabin's windows: a near row of trees, lamps and bushes and
/// a far row of buildings on both sides, going by at the taxi's speed and wrapping
/// round. Each thing is picked at random and set a little off the line, so the street
/// does not repeat in step. Fog hides where things wrap, far ahead and far behind, so
/// nothing pops in. The cabin stands still in its own instance; this is only the look of
/// driving, and only on clients.
/// </summary>
public partial class TaxiView : Node3D
{
    // Placeholders until seen in the game.
    private const float Span = 240f;
    private const float NearSpacing = 6f;
    private const float NearSide = 5.5f;
    private const float FarSpacing = 12f;
    private const float FarSide = 20f;
    private const float Jitter = 1.5f;
    private const float FogDensity = 0.018f;

    private static readonly string[] Near =
    {
        "res://game/props/city_builder_bits/TreeA.tscn",
        "res://game/props/city_builder_bits/TreeB.tscn",
        "res://game/props/city_builder_bits/TreeC.tscn",
        "res://game/props/city_builder_bits/TreeD.tscn",
        "res://game/props/city_builder_bits/TreeE.tscn",
        "res://game/props/city_builder_bits/Streetlight.tscn",
        "res://game/props/city_builder_bits/BushA.tscn",
        "res://game/props/city_builder_bits/BushB.tscn",
        "res://game/props/city_builder_bits/Firehydrant.tscn",
    };

    private static readonly string[] Far =
    {
        "res://game/props/city_builder_bits/BuildingA.tscn",
        "res://game/props/city_builder_bits/BuildingB.tscn",
        "res://game/props/city_builder_bits/BuildingC.tscn",
        "res://game/props/city_builder_bits/BuildingD.tscn",
        "res://game/props/city_builder_bits/BuildingE.tscn",
        "res://game/props/city_builder_bits/BuildingF.tscn",
        "res://game/props/city_builder_bits/BuildingG.tscn",
        "res://game/props/city_builder_bits/BuildingH.tscn",
    };

    private readonly List<Node3D> _passing = new List<Node3D>();
    private readonly List<float> _lines = new List<float>();
    private readonly Random _random = new Random();

    private Godot.Environment? _environment;
    private bool _fogWas;
    private float _fogDensityWas;

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            QueueFree();
            return;
        }

        foreach (float side in new[] { -1f, 1f })
        {
            AddRow(Near, NearSpacing, side * NearSide, 0.3f);
            AddRow(Far, FarSpacing, side * FarSide, 0.25f);
        }

        Fog(true);
    }

    public override void _ExitTree()
    {
        Fog(false);
    }

    // The cabin faces -Z, so the world outside goes by towards +Z.
    public override void _Process(double delta)
    {
        float step = RoboTaxi.Speed * (float)delta;

        for (int i = 0; i < _passing.Count; i++)
        {
            Node3D thing = _passing[i];
            Vector3 at = thing.Position;
            at.Z += step;

            // Wrapped round, it comes back from far ahead a little changed.
            if (at.Z > Span / 2f)
            {
                at.Z -= Span;
                at.X = _lines[i] + Offset(_lines[i]);
                thing.Rotation = new Vector3(0f, Facing(_lines[i]), 0f);
            }

            thing.Position = at;
        }
    }

    // One row down one side; a gap now and then (the chance given), so it is not a wall.
    private void AddRow(string[] kinds, float spacing, float line, float gapChance)
    {
        int count = (int)(Span / spacing);

        for (int i = 0; i < count; i++)
        {
            if (_random.NextDouble() < gapChance)
            {
                continue;
            }

            PackedScene scene = GD.Load<PackedScene>(kinds[_random.Next(kinds.Length)]);
            Node3D thing = scene.Instantiate<Node3D>();

            // Only a view: nothing out there should block or be hit.
            CollisionObject3D? body = thing as CollisionObject3D;

            if (body != null)
            {
                body.CollisionLayer = 0;
            }

            float along = (i * spacing) - (Span / 2f) + ((float)_random.NextDouble() * spacing * 0.4f);
            thing.Position = new Vector3(line + Offset(line), 0f, along);
            thing.Rotation = new Vector3(0f, Facing(line), 0f);
            AddChild(thing);
            _passing.Add(thing);
            _lines.Add(line);
        }
    }

    // Buildings face the road; small things turn any way.
    private float Facing(float line)
    {
        if (Mathf.Abs(line) >= FarSide)
        {
            return line < 0 ? Mathf.Pi / 2f : -Mathf.Pi / 2f;
        }

        return (float)(_random.NextDouble() * Math.PI * 2.0);
    }

    // Away from the road only, so nothing stands in it.
    private float Offset(float line)
    {
        return Mathf.Sign(line) * (float)_random.NextDouble() * Jitter;
    }

    // The world's environment is shared by every zone: fog is set while riding and put
    // back as it was after.
    private void Fog(bool on)
    {
        if (on)
        {
            WorldEnvironment? world = GetTree().Root.FindChild("Environment", true, false) as WorldEnvironment;
            _environment = world?.Environment;

            if (_environment == null)
            {
                return;
            }

            _fogWas = _environment.FogEnabled;
            _fogDensityWas = _environment.FogDensity;
            _environment.FogEnabled = true;
            _environment.FogDensity = FogDensity;
            _environment.FogSkyAffect = 0.3f;
            return;
        }

        if (_environment != null)
        {
            _environment.FogEnabled = _fogWas;
            _environment.FogDensity = _fogDensityWas;
            _environment = null;
        }
    }
}
