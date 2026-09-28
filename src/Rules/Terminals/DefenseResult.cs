namespace MmoGame3d.Rules.Terminals;
public class DefenseResult
{
    public int Cues { get; set; }
    public int Perfect { get; set; }
    public int Good { get; set; }
    public int BestCombo { get; set; }
    public int Points { get; set; }

    public int Missed
    {
        get { return Cues - Perfect - Good; }
    }
}
