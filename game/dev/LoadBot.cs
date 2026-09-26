namespace MmoGame3d.Dev;

using System;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// One load-test bot's legs: wanders like BotDriver (walks, pauses, turns, jumps now and
/// then) and says a line in chat once in a while, through its own input rather than the
/// shared keyboard, so hundreds can run in one process. It finds its own body by the
/// multiplayer of its branch, not by the local-player group, which all bots share.
/// </summary>
public partial class LoadBot : Node, IPlayerInput
{
    private readonly Random _random;
    private double _spellLeft;
    private double _nextLine;
    private bool _jump;
    private Player? _body;

    public LoadBot(int seed)
    {
        _random = new Random(seed);
        _nextLine = 20 + _random.NextDouble() * 40;
    }

    // Parameterless for Godot, which can make any node itself.
    public LoadBot()
        : this(System.Environment.TickCount)
    {
    }

    public Action<string>? Say { get; set; }

    public float Turn { get; private set; }
    public float Forward { get; private set; }
    public float Strafe { get; private set; }

    public bool TakeJump()
    {
        bool jump = _jump;
        _jump = false;
        return jump;
    }

    public override void _Process(double delta)
    {
        if (_body == null || !IsInstanceValid(_body))
        {
            _body = FindBody();

            if (_body == null)
            {
                return;
            }

            _body.InputSource = this;
        }

        _nextLine -= delta;

        if (_nextLine <= 0 && Say != null)
        {
            _nextLine = 30 + _random.NextDouble() * 60;
            Say("load bot checking in");
        }

        _spellLeft -= delta;

        if (_spellLeft > 0)
        {
            return;
        }

        _spellLeft = 1 + _random.NextDouble() * 3;
        int choice = _random.Next(10);
        Forward = choice == 0 ? 0f : 1f;
        Turn = choice == 1 ? 1f : choice == 2 ? -1f : 0f;
        _jump = choice == 3;
    }

    private Player? FindBody()
    {
        // After the server went away there is no peer to be.
        if (!Multiplayer.HasMultiplayerPeer())
        {
            return null;
        }

        long me = Multiplayer.GetUniqueId();

        // This node is under the bot's ClientGame, and the world is next to that.
        Node? world = GetParent()?.GetParent()?.GetNodeOrNull("World");

        if (world == null)
        {
            return null;
        }

        foreach (Node node in world.FindChildren(me.ToString(), "CharacterBody3D", true, false))
        {
            Player? player = node as Player;

            if (player != null)
            {
                return player;
            }
        }

        return null;
    }
}
