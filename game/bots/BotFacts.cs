namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Client;
using MmoGame3d.Rules.Items;

/// <summary>
/// The facts activities provide and steps need, each checked on the client.
/// </summary>
public static class BotFacts
{
    public static readonly BotFact PhoneEquipped = new BotFact("phone equipped", body =>
    {
        ClientView? view = body.View;
        ItemInstance? device = view?.Belongings.Equipped(SlotType.Device);
        return device != null && device.Type == ItemType.Phone;
    });

    public static readonly BotFact Fullscreen = new BotFact("fullscreen", body =>
    {
        DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();
        return mode == DisplayServer.WindowMode.Fullscreen || mode == DisplayServer.WindowMode.ExclusiveFullscreen;
    });
}
