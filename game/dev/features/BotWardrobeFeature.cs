namespace MmoGame3d.Dev.Features;
/// <summary>The wardrobe in bot testing (IBotFeature): the activity that changes a look.</summary>
public sealed class BotWardrobeFeature : IBotFeature
{
    public void AddTo(BotCatalog catalog)
    {
        catalog.Add(new ChangeLookActivity());
    }
}
