namespace MmoGame3d.Data.Social;
public class WhoisRow
{
    public Guid PlayerId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Zone { get; set; } = "";
    public DateTime SavedAt { get; set; }
    public int? Career { get; set; }
    public long CareerXp { get; set; }
    public int CareerRank { get; set; }
    public double SecondsPlayed { get; set; }
    public long Missions { get; set; }
    public long SkillXp { get; set; }
}
