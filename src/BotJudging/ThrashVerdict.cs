namespace MmoGame3d.BotJudging;
/// <summary>What a thrashing check found: the numbers for the finding's line.</summary>
public sealed class ThrashVerdict
{
    public ThrashVerdict(int reversals, float travelled, float net, float widestGap)
    {
        Reversals = reversals;
        Travelled = travelled;
        Net = net;
        WidestGap = widestGap;
    }

    public int Reversals { get; }

    public float Travelled { get; }

    public float Net { get; }

    public float WidestGap { get; }
}
