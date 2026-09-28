namespace MmoGame3d.Dev.Features;
/// <summary>
/// Whois in bot testing, in one file: the activity, registered with the catalog by the
/// feature (IBotFeature). A model for a feature's bot part.
/// </summary>
public sealed class BotWhoisFeature : IBotFeature
{
    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new EditWhoisActivity());
    }
}
