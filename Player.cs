namespace MmoGame3d;

using Godot;

// A player's body. The server moves it, and the owning client only sends which way
// the keys point. The synchronizer carries the position to every client.
public partial class Player : CharacterBody3D
{
    private const float Speed = 5f;

    // Below this the body fell off the ground, and goes back to the middle.
    private const float FallLimit = -10f;

    private static readonly Color OwnColor = new Color(0.25f, 0.5f, 1f);

    private Vector2 _moveInput;
    private Vector2 _sentInput;

    // The node is named after the peer id of the client that owns it.
    public long OwnerPeerId
    {
        get { return long.Parse(Name); }
    }

    private bool IsOwnedHere
    {
        get { return !Multiplayer.IsServer() && OwnerPeerId == Multiplayer.GetUniqueId(); }
    }

    public override void _Ready()
    {
        if (IsOwnedHere)
        {
            GetNode<MeshInstance3D>("Body").MaterialOverride = new StandardMaterial3D { AlbedoColor = OwnColor };
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
    private void SendInput()
    {
        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

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

        if (Position.Y < FallLimit)
        {
            Position = Vector3.Zero;
            Velocity = Vector3.Zero;
        }
    }
}
