namespace MmoGame3d.Rules.Movement;

/// <summary>
/// How keys become a walk. A heading is a yaw in radians about the up axis: 0 faces
/// -Z (Godot's forward), and a positive heading is turned to the left, seen from above.
/// The client turns its heading and sends the resulting direction; the server walks any
/// direction it is sent, at its own speed, after checking it with IsValid.
/// </summary>
public static class Walking
{
    // 150 degrees a second, the old game's turn rate.
    public const float TurnRate = 150f * MathF.PI / 180f;

    // turnInput is -1 (right) to 1 (left).
    public static float Turn(float heading, float turnInput, float deltaSeconds)
    {
        return WrapAngle(heading + (turnInput * TurnRate * deltaSeconds));
    }

    // forward is -1 (back) to 1 (forward); strafe is -1 (left) to 1 (right). The result
    // is never longer than 1, so walking diagonally is no faster than straight.
    public static void Direction(float heading, float forward, float strafe, out float x, out float z)
    {
        float sin = MathF.Sin(heading);
        float cos = MathF.Cos(heading);

        x = (-sin * forward) + (cos * strafe);
        z = (-cos * forward) - (sin * strafe);

        float length = MathF.Sqrt((x * x) + (z * z));

        if (length > 1f)
        {
            x /= length;
            z /= length;
        }
    }

    // What the server accepts from a client. A NaN would spread into the position and
    // from there to every client that sees the body.
    public static bool IsValid(float x, float z, float heading)
    {
        return float.IsFinite(x) && float.IsFinite(z) && float.IsFinite(heading);
    }

    public static float WrapAngle(float angle)
    {
        float wrapped = angle % (2f * MathF.PI);

        if (wrapped > MathF.PI)
        {
            wrapped -= 2f * MathF.PI;
        }
        else if (wrapped < -MathF.PI)
        {
            wrapped += 2f * MathF.PI;
        }

        return wrapped;
    }
}
