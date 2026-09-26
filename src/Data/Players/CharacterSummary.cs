namespace MmoGame3d.Data.Players;

// One character as the character screen lists it.
public class CharacterSummary
{
    public Guid PlayerId { get; set; }
    public int Slot { get; set; }
    public string DisplayName { get; set; } = "";
    public string Look { get; set; } = "";
    public int? Career { get; set; }
    public int CareerRank { get; set; }
    public long CareerXp { get; set; }
    public double SecondsPlayed { get; set; }
    public long Missions { get; set; }
    public long SkillXp { get; set; }
}
