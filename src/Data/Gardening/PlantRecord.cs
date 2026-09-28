namespace MmoGame3d.Data.Gardening;
public class PlantRecord
{
    public long Id { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatorName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Design { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<PlantEvent> History { get; set; } = new List<PlantEvent>();
}
