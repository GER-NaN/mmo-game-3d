namespace MmoGame3d.Players;

using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Movement;
using MmoGame3d.Rules.Social;

/// <summary>
/// A player's body. The server is the authority: it moves every body and writes where
/// each is into NetPosition and NetYaw, which the synchronizer carries to the clients
/// that may see it, 20 times a second.
///
/// How a client draws it, and why:
/// - Your own body is predicted. It moves at once from your keys, by the same Step the
///   server runs, against the zone's own collision, so it is smooth every frame. It is
///   corrected to the server only when they part by a lot (a door, a fall, a wall the
///   server saw) or once you stand still: correcting while walking would pull you back,
///   since the server is always a little behind your keys.
/// - Everyone else is drawn a tenth of a second in the past, between the two updates on
///   either side of that moment (snapshot interpolation), so they move at an even speed.
///   Gliding towards the newest update instead made motion pulse 20 times a second.
/// </summary>
public partial class Player : CharacterBody3D
{
    public const string LocalGroup = "local_player";

    private const float Speed = 5f;
    private const float JumpSpeed = 5f;

    // Below this the body fell off the world, and goes back to the zone's spawn.
    private const float FallLimit = -10f;

    // Past this gap, a body jumps to the server's position at once (a door, a respawn,
    // a prediction gone wrong).
    private const float SnapDistance = 3f;

    // Your own body, once you stand still, closes the gap to the server at this rate a
    // second; and waits this long after the last walk before starting to.
    private const float SettleRate = 6f;
    private const double SettleAfter = 0.25;

    // Others are drawn this far in the past: two updates' worth, so there is nearly
    // always an update on each side of the moment drawn.
    private const double InterpolationDelay = 0.1;
    private const int SnapshotCount = 8;

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
    private double _sinceWalked;
    private Network? _network;

    // Client only: the updates as they arrived, for drawing others in the past.
    private readonly double[] _snapTimes = new double[SnapshotCount];
    private readonly Vector3[] _snapPositions = new Vector3[SnapshotCount];
    private readonly float[] _snapYaws = new float[SnapshotCount];
    private int _snapNewest = -1;
    private int _snapCount;
    private bool _recording;

    // Client only: how the body is seen to move, smoothed, for the animation.
    private CharacterModel? _model;
    private float _seenSpeed;
    private float _seenRise;

    // Server only.
    private double _gestureLeft;
    private Vector2 _moveDirection;
    private float _moveHeading;
    private bool _jumpRequested;

    // Server only: walked on the ground and jumped since the skills last looked.
    private float _walkedMetres;
    private int _jumps;

    private Vector3 _netPosition;
    private float _netYaw;

    [Export]
    public string DisplayName { get; set; } = "";

    // Online by phone: others see the phone in hand, its screen flickering (world.md 7).
    [Export]
    public bool OnPhone { get; set; }

    // HP (see Health). Set by the server; below 50 the walk is slower, on both sides.
    [Export]
    public int Health { get; set; } = Rules.Players.Health.Max;

    // The career and rank, public by design ("Mechanical Engineer · Senior"); "" for none.
    [Export]
    public string CareerTitle { get; set; } = "";

    // At a terminal, in the terminal world. The server holds an online body still; others
    // see it under the name.
    [Export]
    public bool IsOnline { get; set; }

    // What the body is doing with itself for a moment (see Gestures), or empty. Only the
    // server sets it; walking ends it.
    [Export]
    public string GestureId { get; set; } = "";

    private string _look = "a";

    // How the body looks (see Appearance). Synced on change: the wardrobe can change it,
    // and a client redresses the model when it does.
    [Export]
    public string Look
    {
        get
        {
            return _look;
        }

        set
        {
            if (_look == value)
            {
                return;
            }

            _look = value;

            if (_model != null)
            {
                RemoveChild(_model);
                _model.QueueFree();
                _model = new CharacterModel { Name = "Model", Appearance = _look };
                AddChild(_model);
            }
        }
    }

    // The persistent player id, public: party rosters on clients name members by it.
    [Export]
    public string PlayerIdText { get; set; } = "";

    // Set by the server each step; on a client each update is kept for interpolation.
    [Export]
    public Vector3 NetPosition
    {
        get
        {
            return _netPosition;
        }

        set
        {
            _netPosition = value;

            if (_recording)
            {
                Record(value);
            }
        }
    }

    [Export]
    public float NetYaw
    {
        get { return _netYaw; }
        set { _netYaw = value; }
    }

    // Where the server puts a body that fell off the world.
    public Vector3 RespawnPoint { get; set; }

    // The owner's heading: which way the body faces and the chase camera looks.
    public float Heading { get; private set; }

    // True while the owner is asking to walk; the camera eases back behind then.
    public bool IsWalking
    {
        get { return _sentDirection != Vector2.Zero; }
    }

    // False in a load-test process: dozens of clients there would each animate every body
    // they see, thousands of skeletons, and nobody looks. Process-wide, set before any
    // body spawns.
    public static bool DrawModels { get; set; } = true;

    // Load-test bots walk from this instead of the keyboard; null for a person.
    public IPlayerInput? InputSource { get; set; }

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
        _recording = true;
        Record(NetPosition);

        if (DrawModels)
        {
            _model = new CharacterModel { Name = "Model", Appearance = Look };
            AddChild(_model);
        }

        if (IsOwnedHere)
        {
            Heading = NetYaw;
            AddToGroup(LocalGroup);

            // You know your own name; up close (a cabin) it only fills the view.
            GetNode<Label3D>("NameLabel").Visible = false;
        }
    }

    // Client only: how the name label reads. Selected wins over party.
    public void MarkLabel(bool selected, bool partyMember)
    {
        Label3D label = GetNode<Label3D>("NameLabel");

        string name = DisplayName + (CareerTitle.Length > 0 ? "\n" + CareerTitle : "") + (IsOnline ? "\n(online)" : "");

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
        if (Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer())
        {
            Simulate((float)delta);
        }
    }

    // A client draws every frame: your own body from your keys, others from the past.
    public override void _Process(double delta)
    {
        if (!Multiplayer.HasMultiplayerPeer() || Multiplayer.IsServer())
        {
            return;
        }

        Vector3 before = Position;

        if (IsOwnedHere)
        {
            Predict(delta);
            Rotation = new Vector3(0f, Heading, 0f);
        }
        else
        {
            Interpolate();
        }

        Animate(before, (float)delta);
    }

    // The one walking step, the same on server and client: the walk sets the horizontal
    // speed, gravity pulls when off the floor, a jump only leaves the floor.
    private void Step(Vector2 walk, bool jump, float delta)
    {
        Vector3 velocity = Velocity;
        float speed = Rules.Players.Health.IsSlowed(Health) ? Speed * Rules.Players.Health.SlowFactor : Speed;
        velocity.X = walk.X * speed;
        velocity.Z = walk.Y * speed;

        if (IsOnFloor())
        {
            if (jump)
            {
                velocity.Y = JumpSpeed;
            }
        }
        else
        {
            velocity += GetGravity() * delta;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    // Owner only: move now from the keys, send them on, and settle onto the server's
    // position when standing still or far out.
    private void Predict(double delta)
    {
        Vector2 walk;
        bool jump;
        ReadInput(delta, out walk, out jump);

        // Online, the server holds the body at the terminal; predicting a walk would
        // only be corrected back.
        if (IsOnline)
        {
            walk = Vector2.Zero;
            jump = false;
        }

        Step(walk, jump, (float)delta);

        _sinceWalked = walk != Vector2.Zero || !IsOnFloor() ? 0 : _sinceWalked + delta;
        Vector3 gap = NetPosition - Position;

        if (gap.Length() > SnapDistance)
        {
            Position = NetPosition;
            Velocity = Vector3.Zero;
        }
        else if (_sinceWalked >= SettleAfter)
        {
            Position = Position.Lerp(NetPosition, 1f - Mathf.Exp(-SettleRate * (float)delta));
        }
    }

    // Others: the position a tenth of a second ago, between the updates either side.
    private void Interpolate()
    {
        if (_snapCount == 0)
        {
            return;
        }

        double drawAt = Now() - InterpolationDelay;
        int newest = _snapNewest;

        if (drawAt >= _snapTimes[newest] || _snapCount == 1)
        {
            Place(_snapPositions[newest], _snapYaws[newest]);
            return;
        }

        for (int i = 0; i < _snapCount - 1; i++)
        {
            int later = (newest - i + SnapshotCount) % SnapshotCount;
            int earlier = (later - 1 + SnapshotCount) % SnapshotCount;

            if (_snapTimes[earlier] <= drawAt)
            {
                double span = _snapTimes[later] - _snapTimes[earlier];
                float t = span > 0 ? (float)((drawAt - _snapTimes[earlier]) / span) : 1f;

                // A jump between two updates (a respawn) is not walked across.
                if (_snapPositions[earlier].DistanceTo(_snapPositions[later]) > SnapDistance)
                {
                    t = 1f;
                }

                Place(_snapPositions[earlier].Lerp(_snapPositions[later], t), Mathf.LerpAngle(_snapYaws[earlier], _snapYaws[later], t));
                return;
            }
        }

        int oldest = (newest - _snapCount + 1 + SnapshotCount) % SnapshotCount;
        Place(_snapPositions[oldest], _snapYaws[oldest]);
    }

    private void Place(Vector3 position, float yaw)
    {
        Position = position;
        Rotation = new Vector3(0f, yaw, 0f);
    }

    // An update as it arrives, with when. The yaw is the latest known: the synchronizer
    // sets position and yaw one after the other, so this may be one update behind, which
    // is invisible in a turn.
    private void Record(Vector3 position)
    {
        _snapNewest = (_snapNewest + 1) % SnapshotCount;
        _snapTimes[_snapNewest] = Now();
        _snapPositions[_snapNewest] = position;
        _snapYaws[_snapNewest] = _netYaw;
        _snapCount = Mathf.Min(_snapCount + 1, SnapshotCount);
    }

    private static double Now()
    {
        return Time.GetTicksUsec() / 1000000.0;
    }

    // The look follows what the body is seen doing, so it works the same for every body
    // on the screen, yours included.
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

        Gesture? gesture = GestureId.Length > 0 ? Gestures.Find(GestureId) : null;

        _model.ShowPhone(OnPhone);

        if (IsOnline)
        {
            _model.Play(CharacterModel.Busy);
        }
        else if (gesture != null && _seenSpeed <= WalkFrom)
        {
            _model.Play(gesture.Animation);
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

    // The session's Network node, under this client's Main. Found by walking up, since in
    // a load test many Mains share the tree.
    private Network? SessionNetwork()
    {
        if (_network != null)
        {
            return _network;
        }

        Node? node = GetParent();

        while (node != null && node.GetNodeOrNull("Network") == null)
        {
            node = node.GetParent();
        }

        _network = node?.GetNodeOrNull<Network>("Network");
        return _network;
    }

    // Reads the keys (or the input source), turns the heading, and sends the walk on.
    // While a text field or a menu has focus, the keys belong to it, not to walking.
    private void ReadInput(double delta, out Vector2 walk, out bool jump)
    {
        bool keysFree = InputSource == null && GetViewport().GuiGetFocusOwner() == null;
        float turn = keysFree ? Input.GetAxis("turn_right", "turn_left") : 0f;
        float forward = keysFree ? Input.GetAxis("move_back", "move_forward") : 0f;
        float strafe = keysFree ? Input.GetAxis("strafe_left", "strafe_right") : 0f;
        jump = keysFree && Input.IsActionJustPressed("jump");

        if (InputSource != null)
        {
            turn = InputSource.Turn;
            forward = InputSource.Forward;
            strafe = InputSource.Strafe;
            jump = InputSource.TakeJump();
        }

        Heading = Walking.Turn(Heading, turn, (float)delta);

        float x;
        float z;
        Walking.Direction(Heading, forward, strafe, out x, out z);
        walk = new Vector2(x, z);

        Network? network = SessionNetwork();

        if (network == null)
        {
            return;
        }

        if (jump)
        {
            network.SendJump();
        }

        _sinceSend += delta;

        if (walk == Vector2.Zero && _sentDirection != Vector2.Zero)
        {
            network.SendStop(Heading);
            _sentDirection = Vector2.Zero;
            _sentHeading = Heading;
            _sinceSend = 0;
        }
        else if ((walk != _sentDirection || !Mathf.IsEqualApprox(Heading, _sentHeading)) && _sinceSend >= InputSendInterval)
        {
            network.SendWalk(walk, Heading);
            _sentDirection = walk;
            _sentHeading = Heading;
            _sinceSend = 0;
        }
    }

    // Server only, from Network: what the owner asked for, checked here.
    public void ApplyWalk(Vector2 direction, float heading)
    {
        if (Walking.IsValid(direction.X, direction.Y, heading))
        {
            _moveDirection = direction.LimitLength(1f);
            _moveHeading = Walking.WrapAngle(heading);
        }
    }

    public void ApplyStop(float heading)
    {
        if (Walking.IsValid(0f, 0f, heading))
        {
            _moveDirection = Vector2.Zero;
            _moveHeading = Walking.WrapAngle(heading);
        }
    }

    public void ApplyJump()
    {
        _jumpRequested = true;
    }

    public float TakeWalkedMetres()
    {
        float metres = _walkedMetres;
        _walkedMetres = 0f;
        return metres;
    }

    public int TakeJumps()
    {
        int jumps = _jumps;
        _jumps = 0;
        return jumps;
    }

    // Server only: starts a gesture everyone sees. A timed one ends by itself; one with
    // no time (sitting) lasts until the player moves.
    public void Show(string gestureId)
    {
        Gesture? gesture = Gestures.Find(gestureId);

        if (gesture != null)
        {
            GestureId = gesture.Id;
            _gestureLeft = gesture.Seconds;
        }
    }

    private void Simulate(float delta)
    {
        // Online, the body stands at the terminal: the walk it last asked for waits.
        Vector2 walk = IsOnline ? Vector2.Zero : _moveDirection;
        bool jump = _jumpRequested && !IsOnline;
        _jumpRequested = false;

        if (GestureId.Length > 0)
        {
            _gestureLeft -= delta;

            if (walk != Vector2.Zero || jump || (Gestures.Find(GestureId)?.Seconds > 0 && _gestureLeft <= 0))
            {
                GestureId = "";
            }
        }

        Vector3 before = Position;
        bool leaving = jump && IsOnFloor();
        Step(walk, jump, delta);
        Rotation = new Vector3(0f, _moveHeading, 0f);

        if (leaving)
        {
            _jumps++;
        }

        // Only walking on the ground counts: a fall or a respawn is not travel.
        if (walk != Vector2.Zero && IsOnFloor())
        {
            _walkedMetres += new Vector2(Position.X - before.X, Position.Z - before.Z).Length();
        }

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
