namespace MmoGame3d.Dev;

using System.Text;
using Godot;
using MmoGame3d.Players;

/// <summary>
/// Prints what this client sees every few seconds: each player body and where it is.
/// For headless checks, where there is no screen to look at.
/// </summary>
public partial class WorldReport : Node
{
    private double _interval;
    private double _sinceReport;

    public void Start(double intervalSeconds)
    {
        _interval = intervalSeconds;
    }

    public override void _Process(double delta)
    {
        _sinceReport += delta;

        if (_sinceReport < _interval)
        {
            return;
        }

        _sinceReport = 0;
        StringBuilder line = new StringBuilder("Report:");
        Node? world = GetNodeOrNull("/root/Main/World");

        if (world != null)
        {
            // FindChildren matches engine classes, not C# script classes.
            foreach (Node node in world.FindChildren("*", "CharacterBody3D", true, false))
            {
                Player? player = node as Player;

                if (player != null)
                {
                    line.Append(' ').Append(player.DisplayName).Append(' ').Append(player.Position.ToString("F1"));
                }
            }
        }

        GD.Print(line.ToString());
    }
}
