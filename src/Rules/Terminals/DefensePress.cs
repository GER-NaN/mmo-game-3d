namespace MmoGame3d.Rules.Terminals;
public class DefensePress
{
    public DefensePress(int atMs, int lane)
    {
        AtMs = atMs;
        Lane = lane;
    }

    public int AtMs { get; }
    public int Lane { get; }
}
