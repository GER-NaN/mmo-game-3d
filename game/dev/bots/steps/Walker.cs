namespace MmoGame3d.Dev;
using Godot;

/// <summary>
/// Walking to a point: along the navigation path round the buildings where the zone has
/// one (BotNavigation), straight where it has not, and getting unstuck on the way: not
/// moving for a while, it backs off, turns aside, walks on with a jump, and tries again.
/// After a few tries the point is out of reach.
/// </summary>
public sealed class Walker
{
    private const float Moved = 0.5f;
    private const double StuckAfter = 2.5;
    private const int MaxUnsticks = 8;
    private const double RepathEvery = 3;
    private bool _mapShown;
    private const float WaypointReached = 0.8f;

    private Vector3 _lastAt;
    private bool _haveLastAt;
    private double _sinceProgress;
    private double _unstickLeft = -1;
    private Vector3[] _path = System.Array.Empty<Vector3>();
    private int _next;
    private Vector3 _pathTo;
    private double _repathIn;
    private double _closeIn;
    private int _unsticks;
    private bool _turnLeft;

    // Each detour is different, turning further and walking longer as tries mount.
    private double _backFor;
    private double _turnFor;
    private double _onFor;

    // Done at the point (within near), Failed when it cannot get there.
    public StepResult Walk(BotBody body, Vector3 target, float near, double delta)
    {
        if (_unstickLeft >= 0)
        {
            Unstick(body, delta);
            _repathIn = 0;
            return StepResult.Running;
        }

        // Still at a terminal (a goal dropped mid-run leaves it open): off it first, as a
        // person would, since no key walks while it is up.
        if (body.CannotWalk)
        {
            _closeIn -= delta;

            if (_closeIn <= 0)
            {
                _closeIn = 1;
                GD.Print("Bot: closing the screen before walking");
                body.CloseOne();
            }

            return StepResult.Running;
        }

        Players.Player? me = body.Me;

        if (me == null)
        {
            return StepResult.Running;
        }

        if (body.DistanceTo(target) <= near)
        {
            body.Stop();
            return StepResult.Done;
        }

        if (body.Navigation.Pending)
        {
            body.Stop();
            _repathIn = 0;
            return StepResult.Running;
        }

        body.SteerTo(Aim(body, me.GlobalPosition, target, delta));

        // Stuck is not moving: a path round a building takes the bot away from the
        // target for a while, so getting no closer is not stuck.
        if (!_haveLastAt || me.GlobalPosition.DistanceTo(_lastAt) > Moved)
        {
            _lastAt = me.GlobalPosition;
            _haveLastAt = true;
            _sinceProgress = 0;
            return StepResult.Running;
        }

        _sinceProgress += delta;

        if (_sinceProgress < StuckAfter)
        {
            return StepResult.Running;
        }

        if (_unsticks >= MaxUnsticks)
        {
            body.Stop();
            return StepResult.Failed;
        }

        _unsticks++;
        _sinceProgress = 0;
        _turnLeft = body.Random.Next(2) == 0;

        // The side without a door, when one side has one.
        if (body.DoorToward(_turnLeft ? Mathf.Pi / 2f : -Mathf.Pi / 2f, 2f))
        {
            _turnLeft = !_turnLeft;
        }
        _backFor = 0.4 + (body.Random.NextDouble() * 0.6);
        _turnFor = 0.4 + (body.Random.NextDouble() * 0.4 * _unsticks);
        _onFor = 1 + (body.Random.NextDouble() * 0.5 * _unsticks);
        _unstickLeft = _backFor + _turnFor + _onFor;
        GD.Print("Bot: stuck, working round it");
        return StepResult.Running;
    }

    // The next point of the path to the target, asked for again now and then, or the
    // target itself with no path.
    private Vector3 Aim(BotBody body, Vector3 at, Vector3 target, double delta)
    {
        _repathIn -= delta;

        if (_repathIn <= 0 || _pathTo.DistanceTo(target) > 1f)
        {
            _path = body.Navigation.Path(at, target);
            _next = 1;
            _pathTo = target;
            _repathIn = RepathEvery;

            // Straight at the target from here on: worth a line in the log, for a finding.
            // A door is carved out of the mesh 2.5 m round, so a path to one ends short.
            float shortBy = _path.Length == 0 ? float.MaxValue : Flat(_path[_path.Length - 1], target);

            if (body.Navigation.Ready && shortBy > 3f)
            {
                GD.Print("Bot: " + (_path.Length == 0 ? "no path" : "the path ends " + shortBy.ToString("0.0") + " m short") + " from ("
                    + at.X.ToString("0") + ", " + at.Z.ToString("0") + ") to (" + target.X.ToString("0") + ", " + target.Z.ToString("0") + "); straight on; the mesh is "
                    + at.DistanceTo(body.Navigation.Closest(at)).ToString("0.0") + " m from here and " + target.DistanceTo(body.Navigation.Closest(target)).ToString("0.0") + " m from there");

                // The map once per walk, so the log shows what the paths saw.
                if (!_mapShown)
                {
                    _mapShown = true;
                    GD.Print("Bot: the navigation map round it:\n" + body.Navigation.MapAround(at, target));
                }
            }
        }

        while (_next < _path.Length - 1 && Flat(at, _path[_next]) < WaypointReached)
        {
            _next++;
        }

        return _next < _path.Length - 1 ? _path[_next] : target;
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        return new Vector2(a.X - b.X, a.Z - b.Z).Length();
    }

    // Step aside, turn, walk on with a jump. Sideways, not back: backing blind walked
    // bots into the door they had just come out of.
    private void Unstick(BotBody body, double delta)
    {
        body.Stop();
        Input.ActionRelease("strafe_left");
        Input.ActionRelease("strafe_right");
        _unstickLeft -= delta;

        // Never into a door it does not mean to use: a person working round a table does
        // not step out of the shop.
        if (_unstickLeft > _turnFor + _onFor)
        {
            if (!body.DoorToward(_turnLeft ? Mathf.Pi / 2f : -Mathf.Pi / 2f, 2f))
            {
                Input.ActionPress(_turnLeft ? "strafe_left" : "strafe_right");
            }
        }
        else if (_unstickLeft > _onFor)
        {
            Input.ActionPress(_turnLeft ? "turn_left" : "turn_right");
        }
        else if (_unstickLeft > 0)
        {
            if (!body.DoorAhead(2f))
            {
                Input.ActionPress("move_forward");
                Input.ActionPress("jump");
            }
        }
        else
        {
            _unstickLeft = -1;
        }
    }
}
