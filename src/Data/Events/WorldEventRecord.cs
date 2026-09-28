namespace MmoGame3d.Data.Events;
public class WorldEventRecord
{
    public long Id { get; set; }
    public string Line { get; set; } = "";
    public bool Running { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // "completed", "timed-out", "ended-by-restart"; empty while running.
    public string Outcome { get; set; } = "";
    public bool TookPart { get; set; }
}
