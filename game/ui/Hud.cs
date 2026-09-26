namespace MmoGame3d.Ui;

using Godot;

// What stays on screen while playing. It never takes the mouse, so clicks reach the world.
public partial class Hud : Control
{
    public void ShowIdentity(string displayName, string zoneId)
    {
        GetNode<Label>("%Identity").Text = displayName + "   " + zoneId;
    }
}
