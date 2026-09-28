namespace MmoGame3d.Data.Scores;
public class ScoreRecord
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = "";
    public int Score { get; set; }
    public double Seconds { get; set; }
}
