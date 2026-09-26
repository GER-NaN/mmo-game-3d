namespace MmoGame3d.Players;

using Godot;
using MmoGame3d.Rules.Movement;

/// <summary>
/// A player's body. The server moves it; the owning client turns its own heading and
/// sends the direction it wants to walk. The server writes where the body is into
/// NetPosition and NetYaw, the synchronizer carries those to the clients that may see
/// it, and each client glides the body towards them.
///
/// Why synced copies and not position itself: the server sends 20 times a second, not
/// every frame, to keep the bandwidth down. Setting position straight from each update
/// would make every body jump 20 times a second, so the client smooths instead.
/// </summary>
public partial class Player : CharacterBody3D
{
    public const string LocalGroup = "local_player";

    private const float Speed = 5f;
    private const float JumpSpeed = 5f;

    // Below this the body fell off the world, and goes back to the zone's spawn.
    private const float FallLimit = -10f;

    // How fast a body closes the gap to its last synced position, per second; and the
    // gap past which it jumps there at once (a door, a respawn).
    private const float SmoothingRate = 15f;
    private const float SnapDistance = 4f;

    // The owner sends a changing walk at most this often. A stop goes out at once.
    private const double InputSendInterval = 0.05;

    // Speeds, in world units a second, where the look changes from standing to walking
    // to running, and the vertical speed that counts as in the air.
    private const float WalkFrom = 0.3f;
    private const float RunFrom = 3f;
    private const float AirborneFrom = 1.2f;

    // Owner only.
    private Vector2 _sentDirection;
    private float _sentHeading;
    private double _sinceSend;

    // Client only: how the body is seen to move, smoothed, for the animation.
    private CharacterModel? _model;
    private float _seenSpeed;
    private float _seenRise;

    // Server only.
    private Vector2 _moveDirection;
    private float _moveHeading;
    private bool _jumpRequested;

    [Export]
    public string DisplayName { get; set; } = "";

    // At a terminal, in the terminal world. The server holds an online body still; others
    // see it under the name.
    [Export]
    public bool IsOnline { get; set; }

    // The persistent player id, public: party rosters on clients name members by it.
    [Export]
    public string PlayerIdText { get; set; } = "";

    [Export]
    public Vector3 NetPosition { get; set; }

    [Export]
    public float NetYaw { get; set; }

    // Where the server puts a body that fell off the world.
    public Vector3 RespawnPoint { get; set; }

    // The owner's heading: which way the body faces and the chase camera looks.
    public float Heading { get; private set; }

    // True while the owner is asking to walk; the camera eases back behind then.
    public bool IsWalking
    {
        get { return _sentDirection != Vector2.Zero; }
    }

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

        if (Multiplayer.IsServer())
        {
            _moveHeading = Rotation.Y;
            PublishPose();
            return;
        }

        // A spawn arrives with the synced values already set.
        Position = NetPosition;
        Rotation = new Vector3(0f, NetYaw, 0f);

        _model = new CharacterModel { Name = "Model" };
        AddChild(_model);

        if (IsOwnedHere)
        {
            Heading = NetYaw;
            AddToGroup(LocalGroup);
        }
    }

    // Client only: how the name label reads. Selected wins over party.
    public void MarkLabel(bool selected, bool partyMember)
    {
        Label3D label = GetNode<Label3D>("NameLabel");

        string name = DisplayName + (IsOnline ? "\n(online)" : "");

        if (selected)
        {
            label.Modulate = new Color(1f, 0.9f, 0.3f);
            label.Text = "> " + name + " <";
        }
        else
        {
            label.Modulate = partyMember ? new Color(0.5f, 1f, 0.6f) : Colors.White;
            label.Text = name;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer())
        {
            return;
        }

        if (Multiplayer.IsServer())
        {
            Simulate((float)delta);
        }
        else if (IsOwnedHere)
        {
            ReadInput(delta);
        }
    }

    public override void _Process(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer())
        {
            return;
        }

        Vector3 before = Position;

        if (Position.DistanceTo(NetPosition) > SnapDistance)
        {
            Position = NetPosition;
        }
        else
        {
            Position = Position.Lerp(NetPosition, 1f - Mathf.Exp(-SmoothingRate * (float)delta));
        }

        Animate(before, (float)delta);

        // Your own body faces your heading at once; others turn smoothly to theirs.
        float yaw = IsOwnedHere ? Heading : Mathf.LerpAngle(Rotation.Y, NetYaw, 1f - Mathf.Exp(-SmoothingRate * (float)delta));
        Rotation = new Vector3(0f, yaw, 0f);
    }

    // The look follows what the body is seen doing, so it works the same for every body
    // on the screen, yours included, from the synced position alone.
    private void Animate(Vector3 before, float delta)
    {
        if (_model == null || delta <= 0f)
        {
            return;
        }

        Vector3 moved = Position - before;
        float blend = 1f - Mathf.Exp(-10f * delta);
        _seenSpeed = Mathf.Lerp(_seenSpeed, new Vector2(moved.X, moved.Z).Length() / delta, blend);
        _seenRise = Mathf.Lerp(_seenRise, moved.Y / delta, blend);

        if (IsOnline)
        {
            _model.Play(CharacterModel.Busy);
        }
        else if (Mathf.Abs(_seenRise) > AirborneFrom)
        {
            _model.Play(CharacterModel.Airborne);
        }
        else if (_seenSpeed > RunFrom)
        {
            _model.Play(CharacterModel.Run);
        }
        else if (_seenSpeed > WalkFrom)
        {
            _model.Play(CharacterModel.Walk);
        }
        else
        {
            _model.Play(CharacterModel.Idle);
        }
    }

    // While a text field or a menu has focus, the keys belong to it, not to walking.
    private void ReadInput(double delta)
    {
        bool keysFree = GetViewport().GuiGetFocusOwner() == null;
        float turn = keysFree ? Input.GetAxis("turn_right", "turn_left") : 0f;
        float forward = keysFree ? Input.GetAxis("move_back", "move_forward") : 0f;
        float strafe = keysFree ? Input.GetAxis("strafe_left", "strafe_right") : 0f;

        Heading = Walking.Turn(Heading, turn, (float)delta);

        float x;
        float z;
        Walking.Direction(Heading, forward, strafe, out x, out z);
        Vector2 direction = new Vector2(x, z);

        if (keysFree && Input.IsActionJustPressed("jump"))
        {
            RpcId(1, MethodName.Jump);
        }

        _sinceSend += delta;

        if (direction == Vector2.Zero && _sentDirection != Vector2.Zero)
        {
            // Reliable, so a lost packet cannot leave the body walking.
            RpcId(1, MethodName.StopWalking, Heading);
            _sentDirection = Vector2.Zero;
            _sentHeading = Heading;
            _sinceSend = 0;
        }
        else if ((direction != _sentDirection || !Mathf.IsEqualApprox(Heading, _sentHeading)) && _sinceSend >= InputSendInterval)
        {
            RpcId(1, MethodName.Walk, direction, Heading);
            _sentDirection = direction;
            _sentHeading = Heading;
            _sinceSend = 0;
        }
    }

    // Unreliable but ordered: a lost walk is replaced by the next one 50 ms later, and an
    // old one never overtakes a newer one.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void Walk(Vector2 direction, float heading)
    {
        if (IsFromOwner() && Walking.IsValid(direction.X, direction.Y, heading))
        {
            _moveDirection = direction.LimitLength(1f);
            _moveHeading = Walking.WrapAngle(heading);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StopWalking(float heading)
    {
        if (IsFromOwner() && Walking.IsValid(0f, 0f, heading))
        {
            _moveDirection = Vector2.Zero;
            _moveHeading = Walking.WrapAngle(heading);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Jump()
    {
        if (IsFromOwner())
        {
            _jumpRequested = true;
        }
    }

    // Closed by default: only the server acts on these, and only for the body's owner.
    private bool IsFromOwner()
    {
        return Multiplayer.IsServer() && Multiplayer.GetRemoteSenderId() == OwnerPeerId;
    }

    private void Simulate(float delta)
    {
        // Online, the body stands at the terminal: the walk it last asked for waits.
        Vector2 walk = IsOnline ? Vector2.Zero : _moveDirection;

        if (IsOnline)
        {
            _jumpRequested = false;
        }

        Vector3 velocity = Velocity;
        velocity.X = walk.X * Speed;
        velocity.Z = walk.Y * Speed;

        if (IsOnFloor())
        {
            if (_jumpRequested)
            {
                velocity.Y = JumpSpeed;
            }
        }
        else
        {
            velocity += GetGravity() * delta;
        }

        _jumpRequested = false;
        Velocity = velocity;
        MoveAndSlide();
        Rotation = new Vector3(0f, _moveHeading, 0f);

        if (Position.Y < FallLimit)
        {
            Position = RespawnPoint;
            Velocity = Vector3.Zero;
        }

        PublishPose();
    }

    private void PublishPose()
    {
        NetPosition = Position;
        NetYaw = Rotation.Y;
    }
}
