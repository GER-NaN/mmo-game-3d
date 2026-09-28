namespace MmoGame3d.Dev;
using Godot;

/// <summary>
/// Out of a trap (a gap between buildings, a corner): tries eight directions, straight
/// back first, then the sides, a few seconds each with a jump, until one takes the bot
/// well clear of where it was. Walking at a target keeps pushing it deeper in.
/// </summary>
public sealed class EscapeStep : BotStep
{
    private const double TryFor = 2.5;
    private const float Clear = 2.5f;

    // Turns from the way it faces, in the order tried.
    private static readonly float[] Turns = { Mathf.Pi, Mathf.Pi / 2f, -Mathf.Pi / 2f, Mathf.Pi * 0.75f, -Mathf.Pi * 0.75f, Mathf.Pi / 4f, -Mathf.Pi / 4f, 0f };

    private int _try;
    private double _left;
    private Vector3 _from;
    private Vector3 _toward;

    public EscapeStep()
        : base("get unstuck", (TryFor * 8) + 2)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override void Begin(BotBody body)
    {
        _try = 0;
        Aim(body);
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        Players.Player? me = body.Me;

        if (me == null)
        {
            return StepResult.Failed;
        }

        if (me.GlobalPosition.DistanceTo(_from) > Clear)
        {
            body.Stop();
            GD.Print("Bot: got clear, going " + (int)Mathf.RadToDeg(Turns[_try]) + " degrees from where it faced");
            return StepResult.Done;
        }

        body.SteerTo(_toward);
        Input.ActionPress("jump");
        _left -= delta;

        if (_left <= 0)
        {
            _try++;

            if (_try >= Turns.Length)
            {
                body.Stop();
                return StepResult.Failed;
            }

            Aim(body);
        }

        return StepResult.Running;
    }

    private void Aim(BotBody body)
    {
        Players.Player? me = body.Me;

        if (me == null)
        {
            return;
        }

        _from = me.GlobalPosition;
        _left = TryFor;
        float heading = me.Heading + Turns[_try];
        _toward = _from + (new Vector3(-Mathf.Sin(heading), 0f, -Mathf.Cos(heading)) * 10f);
    }
}
