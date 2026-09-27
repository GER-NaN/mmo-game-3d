namespace MmoGame3d.Town;

using System;
using Godot;
using MmoGame3d.Interact;
using MmoGame3d.Players;
using MmoGame3d.Rules.Town;

/// <summary>
/// Someone who lives in town and walks a loop round a block, stopping now and then. The
/// server walks them (Stroll); only how far along the loop they are, and whether they are
/// walking, is synced, and a client walks them on at the same pace between updates.
/// Talking to them is a line of small talk: they know nothing of the fight.
/// </summary>
public partial class Townsperson : Interactable
{
    // Past this many metres behind or ahead of the server, a client puts them there at once.
    private const float SnapGap = 4f;

    private Stroll? _stroll;
    private CharacterModel? _model;
    private float _shown;
    private bool _placed;

    // The name of the loop they walk, a Path3D under the zone's "Routes".
    [Export]
    public string RouteName { get; set; } = "";

    // Where on the loop they start, in metres.
    [Export]
    public float StartAt { get; set; }

    [Export]
    public string PersonName { get; set; } = "";

    [Export]
    public string ModelPath { get; set; } = "";

    [Export]
    public float Along { get; set; }

    [Export]
    public bool Walking { get; set; } = true;

    // While someone talks to them they stand and face that spot.
    [Export]
    public bool Listening { get; set; }

    [Export]
    public Vector3 ListeningTo { get; set; }

    // How long they stand when talked to. A placeholder.
    private const double TalkSeconds = 6;

    private double _listenLeft;

    public override string Prompt
    {
        get { return "Talk to " + PersonName; }
    }

    // On the server, when a player talks to them.
    public void Listen(Vector3 speaker)
    {
        _stroll?.Stop(TalkSeconds);
        _listenLeft = TalkSeconds;
        Listening = true;
        ListeningTo = speaker;
    }

    private Path3D Route
    {
        get { return GetNode<Path3D>("../../Routes/" + RouteName); }
    }

    public override void _Ready()
    {
        GetNode<Label3D>("NameLabel").Text = PersonName;

        if (Multiplayer.IsServer())
        {
            _stroll = new Stroll(Route.Curve.GetBakedLength(), StartAt, new Random(PersonName.GetHashCode()));
            Along = _stroll.Along;
        }
        else
        {
            _model = new CharacterModel { Name = "Model", ModelPath = ModelPath };
            AddChild(_model);
        }
    }

    public override void _Process(double delta)
    {
        Path3D route = Route;
        float length = route.Curve.GetBakedLength();

        if (_stroll != null)
        {
            _listenLeft -= delta;
            Listening = _listenLeft > 0;
            _stroll.Advance(delta);
            Along = _stroll.Along;
            Walking = _stroll.Walking;
            _shown = Along;
        }
        else
        {
            WalkOn(length, (float)delta);
            _model?.Play(Walking ? CharacterModel.Walk : CharacterModel.Idle);
        }

        Transform = route.Transform * route.Curve.SampleBakedWithRotation(_shown, false, false);

        if (Listening)
        {
            Vector3 toward = new Vector3(ListeningTo.X, GlobalPosition.Y, ListeningTo.Z);

            if (toward.DistanceSquaredTo(GlobalPosition) > 0.01f)
            {
                LookAt(toward, Vector3.Up);
            }
        }
    }

    // On a client: on at the same pace, leaning towards the server's number. The gap is
    // taken the short way round, since the loop wraps from its end to its start.
    private void WalkOn(float length, float delta)
    {
        if (!_placed)
        {
            _shown = Along;
            _placed = true;
            return;
        }

        if (Walking)
        {
            _shown = (_shown + (Stroll.Pace * delta)) % length;
        }

        float gap = Along - _shown;

        if (gap > length / 2f)
        {
            gap -= length;
        }
        else if (gap < -length / 2f)
        {
            gap += length;
        }

        // Far out (the first update after the zone loaded, which starts everyone at the
        // loop's start): jump there, rather than glide across town.
        if (Mathf.Abs(gap) > SnapGap)
        {
            _shown = Along;
            return;
        }

        _shown = Mathf.PosMod(_shown + (gap * 0.05f), length);
    }
}
