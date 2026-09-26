namespace MmoGame3d.Client;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Players;
using MmoGame3d.Ui;

/// <summary>
/// The client's side of parties while in the world: picking a target, inviting,
/// answering an invite, the party panel, and marking party members' name labels.
/// It lives as long as the world does; PartyNetwork lives longer, so every event is
/// unsubscribed on exit.
/// </summary>
public partial class ClientParty : Node
{
    private static readonly PackedScene TargetScene = GD.Load<PackedScene>("res://game/ui/TargetFrame.tscn");
    private static readonly PackedScene PromptScene = GD.Load<PackedScene>("res://game/ui/InvitePrompt.tscn");
    private static readonly PackedScene PanelScene = GD.Load<PackedScene>("res://game/ui/PartyPanel.tscn");

    // Name labels are re-marked this often, which also catches bodies that spawned since.
    private const double MarkInterval = 0.25;

    private readonly HashSet<string> _memberIds = new HashSet<string>();

    private PartyNetwork _network = null!;
    private CanvasLayer _ui = null!;
    private Node _world = null!;
    private TargetFrame? _frame;
    private InvitePrompt? _prompt;
    private PartyPanel? _panel;
    private Player? _target;
    private double _sinceMark;

    // Raised when Give is pressed for the selected player.
    public event System.Action<Player>? GiveRequested;
    public event System.Action<Player>? FriendRequested;
    public event System.Action<Player>? IgnoreRequested;

    public void Start(PartyNetwork network, CanvasLayer ui, Node world)
    {
        _network = network;
        _ui = ui;
        _world = world;

        TargetPicker picker = new TargetPicker { Name = "TargetPicker" };
        AddChild(picker);
        picker.Picked += OnPicked;

        _network.InviteReceived += OnInviteReceived;
        _network.PartyReceived += OnPartyReceived;
    }

    public void SendChat(string text)
    {
        _network.SendChat(text);
    }

    public override void _ExitTree()
    {
        _network.InviteReceived -= OnInviteReceived;
        _network.PartyReceived -= OnPartyReceived;
        _frame?.QueueFree();
        _prompt?.QueueFree();
        _panel?.QueueFree();
    }

    public override void _Process(double delta)
    {
        // The target walked out of sight, left, or changed zone.
        if (_target != null && !IsInstanceValid(_target))
        {
            OnPicked(null);
        }

        _sinceMark += delta;

        if (_sinceMark >= MarkInterval)
        {
            _sinceMark = 0;
            MarkLabels();
        }
    }

    private void OnPicked(Player? player)
    {
        _target = player;

        if (player == null)
        {
            _frame?.QueueFree();
            _frame = null;
        }
        else
        {
            if (_frame == null)
            {
                _frame = TargetScene.Instantiate<TargetFrame>();
                _ui.AddChild(_frame);
                _frame.InvitePressed += OnInvitePressed;
                _frame.GivePressed += () =>
                {
                    if (_target != null && IsInstanceValid(_target))
                    {
                        GiveRequested?.Invoke(_target);
                    }
                };
                _frame.FriendPressed += () =>
                {
                    if (_target != null && IsInstanceValid(_target))
                    {
                        FriendRequested?.Invoke(_target);
                    }
                };
                _frame.IgnorePressed += () =>
                {
                    if (_target != null && IsInstanceValid(_target))
                    {
                        IgnoreRequested?.Invoke(_target);
                    }
                };
            }

            _frame.ShowTarget(player.DisplayName, !_memberIds.Contains(player.PlayerIdText));
        }

        MarkLabels();
    }

    private void OnInvitePressed()
    {
        if (_target != null && IsInstanceValid(_target))
        {
            _network.SendInvite(_target.OwnerPeerId);
        }
    }

    private void OnInviteReceived(string inviterId, string inviterName)
    {
        _prompt?.QueueFree();
        _prompt = PromptScene.Instantiate<InvitePrompt>();
        _ui.AddChild(_prompt);
        _prompt.ShowInvite(inviterName);
        _prompt.Answered += join =>
        {
            _network.SendResponse(inviterId, join);
            _prompt = null;
        };
    }

    private void OnPartyReceived(string leaderId, string[] ids, string[] names, int[] online)
    {
        _memberIds.Clear();

        foreach (string id in ids)
        {
            _memberIds.Add(id);
        }

        GD.Print("Party: " + (ids.Length == 0 ? "none" : string.Join(", ", names)));

        if (ids.Length == 0)
        {
            _panel?.QueueFree();
            _panel = null;
        }
        else
        {
            if (_panel == null)
            {
                _panel = PanelScene.Instantiate<PartyPanel>();
                _ui.AddChild(_panel);
                _panel.LeavePressed += _network.SendLeave;
            }

            _panel.ShowMembers(leaderId, ids, names, online);
        }

        if (_target != null && IsInstanceValid(_target))
        {
            _frame?.ShowTarget(_target.DisplayName, !_memberIds.Contains(_target.PlayerIdText));
        }

        MarkLabels();
    }

    private void MarkLabels()
    {
        // FindChildren matches engine classes, not C# script classes.
        foreach (Node node in _world.FindChildren("*", "CharacterBody3D", true, false))
        {
            Player? player = node as Player;

            if (player != null)
            {
                player.MarkLabel(player == _target, _memberIds.Contains(player.PlayerIdText) && !player.IsInGroup(Player.LocalGroup));
            }
        }
    }
}
