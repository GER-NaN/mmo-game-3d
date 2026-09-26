namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Networking;
using MmoGame3d.Players;
using MmoGame3d.Rules.Terminals;

/// <summary>
/// One load-test bot's legs: wanders like BotDriver (walks, pauses, turns, jumps now and
/// then) and says a line in chat once in a while, through its own input rather than the
/// shared keyboard, so hundreds can run in one process. It finds its own body by the
/// multiplayer of its branch, not by the local-player group, which all bots share.
///
/// With a load scenario (--load-scenario) it keeps doing one thing instead, to load one
/// part of the server: load-phone goes online by phone and stays; load-defense plays
/// short Agent Defense runs back to back, every cue hit; load-taxi calls a taxi once;
/// load-chat talks every second or two. For load, not tests: it calls the networks
/// directly rather than through input.
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
    public string? Scenario { get; set; }
    public Networks? Networks { get; set; }

    private double _actIn = 2;
    private bool _acted;
    private int _seed = -1;
    private int _lengthMs;

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

            if (Scenario != null && Networks != null)
            {
                Networks.Terminal.DefenseSeedReceived += (seed, length) =>
                {
                    _seed = seed;
                    _lengthMs = length;
                    _actIn = (length / 1000.0) + 3.5;
                };
            }
        }

        if (Scenario != null && Networks != null)
        {
            Act(delta, Networks);
            return;
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

    private void Act(double delta, Networks networks)
    {
        _actIn -= delta;

        if (_actIn > 0)
        {
            return;
        }

        switch (Scenario)
        {
            case "load-phone":
                if (!_acted)
                {
                    networks.Items.SendUsePhone();
                    _acted = true;
                }

                _actIn = 1e9;
                break;
            case "load-defense":
                if (!_acted)
                {
                    networks.Items.SendUsePhone();
                    _acted = true;
                    _actIn = 2;
                    return;
                }

                if (_seed >= 0)
                {
                    // Every cue on the beat: the server scores the full chart.
                    List<DefenseCue> chart = AgentDefense.Chart(_seed, _lengthMs);
                    int[] presses = new int[chart.Count];

                    for (int i = 0; i < chart.Count; i++)
                    {
                        presses[i] = AgentDefense.Pack(new DefensePress(chart[i].AtMs, chart[i].Lane));
                    }

                    networks.Terminal.SendDefenseFinish(presses);
                    _seed = -1;
                }

                networks.Terminal.SendDefenseStart();
                _actIn = 30;
                break;
            case "load-taxi":
                if (!_acted)
                {
                    networks.Session.SendInteract("TaxiStand");
                    _acted = true;
                }

                _actIn = 1e9;
                break;
            case "load-chat":
                Say?.Invoke("load chat " + _random.Next(1000));
                _actIn = 1 + _random.NextDouble();
                break;
        }
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
