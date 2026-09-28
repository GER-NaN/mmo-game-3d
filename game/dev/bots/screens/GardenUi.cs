namespace MmoGame3d.Dev.Screens;

using Godot;
using MmoGame3d.Gardening;

/// <summary>
/// The potting table (GardenScreen), worked as a person works it: a piece dragged from the
/// list to a spot on the soil, Complete, a name typed. The only bot code that knows this
/// screen's groups and layout.
/// </summary>
public static class GardenUi
{
    // Between the press, the move and the release of a drag.
    private const double DragBeat = 0.3;

    public static GardenScreen? Screen(BotBody body)
    {
        Node? complete = body.Tree.GetFirstNodeInGroup(GardenScreen.CompleteGroup);
        return complete?.FindParent("GardenScreen") as GardenScreen;
    }

    public static bool IsOpen(BotBody body)
    {
        return Screen(body) != null;
    }

    // How many kinds of piece the list offers.
    public static int PieceKinds(BotBody body)
    {
        return body.Tree.GetNodesInGroup(GardenScreen.PieceGroup).Count;
    }

    // Drags the piece at this place in the list to a spot on the soil (x and z in soil
    // radii, the middle at 0, 0).
    public static BotStep Place(int piece, float x, float z)
    {
        int phase = 0;
        double wait = 0;
        return new DoStep("plant piece " + piece + " at " + x.ToString("0.0") + ", " + z.ToString("0.0"), 6, (b, d) =>
        {
            GardenScreen? screen = Screen(b);

            if (screen == null)
            {
                return StepResult.Failed;
            }

            wait -= d;

            if (wait > 0)
            {
                return StepResult.Running;
            }

            wait = DragBeat;
            Vector2 spot = screen.ScreenPointOnSoil(x, z);

            switch (phase)
            {
                case 0:
                    Godot.Collections.Array<Node> pieces = b.Tree.GetNodesInGroup(GardenScreen.PieceGroup);

                    if (pieces.Count == 0)
                    {
                        return StepResult.Failed;
                    }

                    Vector2 at = ((Control)pieces[piece % pieces.Count]).GetGlobalRect().GetCenter();
                    Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at, GlobalPosition = at });
                    break;
                case 1:
                    Input.ParseInputEvent(new InputEventMouseMotion { Position = spot, GlobalPosition = spot, Relative = Vector2.One });
                    break;
                default:
                    Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = spot, GlobalPosition = spot });
                    return StepResult.Done;
            }

            phase++;
            return StepResult.Running;
        });
    }

    // Complete, then the name typed into the naming line (Enter sends it).
    public static BotStep Complete(string name)
    {
        bool clicked = false;
        double wait = 0;
        return new DoStep("complete as \"" + name + "\"", 6, (b, d) =>
        {
            wait -= d;

            if (wait > 0)
            {
                return StepResult.Running;
            }

            if (!clicked)
            {
                Button? complete = b.Usable(GardenScreen.CompleteGroup);

                if (complete == null)
                {
                    return StepResult.Running;
                }

                GD.Print("Bot: clicking Complete");
                b.Click(complete);
                clicked = true;
                wait = 0.6;
                return StepResult.Running;
            }

            BotDriver.Type(name);
            return StepResult.Done;
        });
    }

    // Until the table says the plant is made.
    public static BotStep WaitDone()
    {
        return new DoStep("wait for the plant", 8, (b, d) =>
        {
            GardenScreen? screen = Screen(b);
            return screen != null && screen.IsDone ? StepResult.Done : StepResult.Running;
        });
    }
}
