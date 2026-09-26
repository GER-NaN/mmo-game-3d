namespace MmoGame3d.Items;

using System;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Items;

/// <summary>
/// Something lying in a zone, picked up by walking over it. The server owns it and
/// notices the touch; a client only draws it. What it is travels once, at spawn.
/// </summary>
public partial class GroundItem : Node3D
{
    // Placeholders until real models: a spinning box, shaped by type, coloured by tier.
    private const float SpinRadiansPerSecond = 1.5f;

    private static readonly Color[] TierColors =
    {
        new Color(0.75f, 0.75f, 0.75f),
        new Color(0.3f, 0.8f, 0.35f),
        new Color(0.3f, 0.5f, 1f),
        new Color(0.75f, 0.35f, 0.95f),
    };

    private bool _taken;

    // Raised on the server when a player walks into it.
    public event Action<GroundItem, Player>? Touched;

    // Synced as ints: the synchronizer carries engine types, not C# enums.
    [Export]
    public int TypeId { get; set; }

    [Export]
    public int TierId { get; set; }

    [Export]
    public int Quantity { get; set; }

    public ItemType Type
    {
        get { return (ItemType)TypeId; }
    }

    public ItemTier Tier
    {
        get { return (ItemTier)TierId; }
    }

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    public override void _Ready()
    {
        Area3D area = GetNode<Area3D>("PickupArea");

        if (Multiplayer.IsServer())
        {
            area.BodyEntered += OnBodyEntered;
            SetProcess(false);
            return;
        }

        area.Monitoring = false;
        GetNode<MeshInstance3D>("Mesh").Mesh = BuildMesh();
    }

    public override void _Process(double delta)
    {
        RotateY(SpinRadiansPerSecond * (float)delta);
    }

    // Two players can touch it in one frame; only the first gets it.
    public void MarkTaken()
    {
        _taken = true;
    }

    private void OnBodyEntered(Node3D body)
    {
        Player? player = body as Player;

        if (!_taken && player != null)
        {
            Touched?.Invoke(this, player);
        }
    }

    private Mesh BuildMesh()
    {
        Vector3 size = Type == ItemType.RamStick ? new Vector3(0.6f, 0.08f, 0.18f) : new Vector3(0.4f, 0.12f, 0.4f);
        int tier = Math.Clamp(TierId, 0, TierColors.Length - 1);

        return new BoxMesh
        {
            Size = size,
            Material = new StandardMaterial3D { AlbedoColor = TierColors[tier], EmissionEnabled = true, Emission = TierColors[tier] * 0.3f },
        };
    }
}
