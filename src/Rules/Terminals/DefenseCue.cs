namespace MmoGame3d.Rules.Terminals;
public class DefenseCue
{
    public DefenseCue(int atMs, int lane)
    {
        AtMs = atMs;
        Lane = lane;
    }

    public int AtMs { get; }
    public int Lane { get; }
}
