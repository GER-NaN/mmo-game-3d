namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Rules.Gardening;

/// <summary>
/// Plants a few pieces at the potting table (GardenScreen open): picks a family's tab and
/// a piece at random, and clicks it. Pressing a piece takes it into the middle of the
/// pot, and letting go there plants it, so a click plants a piece in the middle; placing
/// it elsewhere would be a drag. Each piece is seen on the table's count before the next.
/// </summary>
public class PlantPiecesStep : BotStep
{
    private const double Pause = 0.6;

    private int _wanted;
    private int _before;
    private double _sinceClick = Pause;
    private bool _tabNext = true;

    public PlantPiecesStep()
        : base("plant some pieces", 40)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override void Start(BotBody body)
    {
        _wanted = 1 + body.Random.Next(PlantDesign.MaxPieces);
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        GardenScreen? garden = body.Find<GardenScreen>();
        _sinceClick += delta;

        if (garden == null || _sinceClick < Pause)
        {
            return BotStepState.Running;
        }

        _sinceClick = 0;

        if (garden.PieceCount >= _wanted)
        {
            body.Events.Write("planted", garden.PieceCount + " pieces");
            return BotStepState.Done;
        }

        // A family's tab first, then a piece from it once it shows.
        if (_tabNext)
        {
            _tabNext = false;
            string family = PlantParts.PieceFamilies[body.Random.Next(PlantParts.PieceFamilies.Length)][0];
            Button? tab = BotScreens.FirstButton(garden, family);

            if (tab != null)
            {
                body.Click(tab);
            }

            return BotStepState.Running;
        }

        if (garden.PieceCount > _before)
        {
            _before = garden.PieceCount;
            _tabNext = true;
            return BotStepState.Running;
        }

        List<Button> pieces = new List<Button>();

        foreach (Node node in garden.GetTree().GetNodesInGroup(GardenScreen.PieceGroup))
        {
            Button? piece = node as Button;

            if (piece != null && piece.IsVisibleInTree() && !piece.IsQueuedForDeletion())
            {
                pieces.Add(piece);
            }
        }

        if (pieces.Count > 0)
        {
            body.Click(pieces[body.Random.Next(pieces.Count)]);
        }

        return BotStepState.Running;
    }
}
