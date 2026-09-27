namespace MmoGame3d.BotJudging;

using System.Numerics;

/// <summary>
/// One look at a bot's body, as its player sees it: when, where, whether the bot meant to
/// walk then, how far the client and the server had the body apart, and the keys held.
/// The judges decide from a run of these, so a test can hand them any run it likes.
/// </summary>
public sealed class TrackSample
{
    public TrackSample(double time, Vector3 at, bool walking)
    {
        Time = time;
        At = at;
        Walking = walking;
    }

    public double Time { get; }

    public Vector3 At { get; }

    public bool Walking { get; }

    public float Gap { get; set; }

    public string Keys { get; set; } = "";

    // Across the ground only: up and down (a jump, a bob against a wall) is not travel.
    public static Vector3 Flat(Vector3 move)
    {
        return new Vector3(move.X, 0f, move.Z);
    }
}
