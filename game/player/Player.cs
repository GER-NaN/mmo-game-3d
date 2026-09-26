namespace MmoGame3d.Players;

using Godot;

/// <summary>
/// A player's body. The server moves it; the owning client only sends which way the
/// keys point. The synchronizer carries position and facing to the clients that the
/// server lets see this player (it is hidden by default, see ServerGame).
/// </summary>
public partial class Player : CharacterBody3D
{
    public const string LocalGroup = "local_player";

    private const float Speed = 5f;

    // Below this the body fell off the world, and goes back to the zone's spawn.
    private const float FallLimit = -10f;

    private static readonly Color OwnColor = new Color(0.25f, 0.5f, 1f);

    private Vector2 _moveInput;
    private Vector2 _sentInput;

    // Synced once, when the player spawns on a client.
    [Export]
    public string DisplayName { get; set; } = "";

    // Where the server puts a body that fell off the world.
    public Vector3 RespawnPoint { get; set; }

    // The node is named after the peer id of the client that owns it.
    public long OwnerPeerId
    {
        get { return long.Parse(Name); }
    }

    private bool IsOwnedHere
    {
        get { return !Multiplayer.IsServer() && OwnerPeerId == Multiplayer.GetUniqueId(); }
    }

    public MultiplayerSynchronizer Synchronizer
    {
        get { return GetNode<MultiplayerSynchronizer>("Synchronizer"); }
    }

    public override void _Ready()
    {
        GetNode<Label3D>("NameLabel").Text = DisplayName;

        if (IsOwnedHere)
        {
            GetNode<MeshInstance3D>("Body").MaterialOverride = new StandardMaterial3D { AlbedoColor = OwnColor };
            AddToGroup(LocalGroup);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.IsServer())
        {
            Move((float)delta);
        }
        else if (IsOwnedHere)
        {
            SendInput();
        }
    }

    // Sent only when it changes, and reliably, so a lost packet cannot leave a key held.
    // While a text field or a menu has focus, the keys belong to it, not to walking.
    private void SendInput()
    {
        Vector2 input = Vector2.Zero;

        if (GetViewport().GuiGetFocusOwner() == null)
        {
            input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        }

        if (input != _sentInput)
        {
            _sentInput = input;
            RpcId(1, MethodName.SetMoveInput, input);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SetMoveInput(Vector2 input)
    {
        // Closed by default: only the owner steers this body, and never faster than full.
        if (!Multiplayer.IsServer() || Multiplayer.GetRemoteSenderId() != OwnerPeerId)
        {
            return;
        }

        _moveInput = input.LimitLength(1f);
    }

    private void Move(float delta)
    {
        Vector3 velocity = Velocity;
        velocity.X = _moveInput.X * Speed;
        velocity.Z = _moveInput.Y * Speed;

        if (!IsOnFloor())
        {
            velocity += GetGravity() * delta;
        }

        Velocity = velocity;
        MoveAndSlide();

        // Face the way the body walks. The model's front is -Z, Godot's forward.
        if (_moveInput != Vector2.Zero)
        {
            Rotation = new Vector3(0f, Mathf.Atan2(-_moveInput.X, -_moveInput.Y), 0f);
        }

        if (Position.Y < FallLimit)
        {
            Position = RespawnPoint;
            Velocity = Vector3.Zero;
        }
    }
}
