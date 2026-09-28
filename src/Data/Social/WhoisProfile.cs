namespace MmoGame3d.Data.Social;
using MmoGame3d.Rules.Social;

public class WhoisProfile
{
    public WhoisRow Row { get; set; } = new WhoisRow();
    public WhoisSettings Settings { get; set; } = new WhoisSettings();
    public Dictionary<int, long> Skills { get; } = new Dictionary<int, long>();
    public int Props { get; set; }
    public bool GavePropsToo { get; set; }
    public int Friends { get; set; }
}
