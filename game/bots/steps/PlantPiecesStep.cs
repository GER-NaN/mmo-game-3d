namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Rules.Gardening;

/// <summary>
/// Plants a few pieces at the potting table (GardenScreen open): steps the kind of plant
/// on a random number of times, then clicks one of the pieces showing. Pressing a piece takes it into the middle of the
/// pot, and letting go there plants it, so a click plants a piece in the middle; placing
/// it elsewhere would be a drag. Each piece is seen on the table's count before the next.
/// </summary>
public class PlantPiecesStep : BotStep
{
    private const double Pause = 0.6;

    private int _wanted;
    private int _before;
    private double _sinceClick = Pause;
    private bool _kindNext = true;
    private int _kindSteps;

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

        // A kind of plant first, a click on "next kind" at a time, then a piece of it.
        if (_kindNext)
        {
            _kindNext = false;
            _kindSteps = body.Random.Next(PlantParts.PieceFamilies.Length);
        }

        if (_kindSteps > 0)
        {
            _kindSteps--;
            Button? down = BotScreens.Named<Button>(garden, "KindDown");

            if (down != null)
            {
                body.Click(down);
            }

            return BotStepState.Running;
        }

        if (garden.PieceCount > _before)
        {
            _before = garden.PieceCount;
            _kindNext = true;
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
