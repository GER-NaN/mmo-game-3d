namespace MmoGame3d.Bots;

using Godot;
using MmoGame3d.Client;
using MmoGame3d.Rules.Items;

/// <summary>
/// The facts activities provide and steps need, each checked on the client through its
/// view of the player (ClientView).
/// </summary>
public static class BotFacts
{
    public static readonly BotFact PhoneEquipped = new BotFact("phone-equipped", "phone equipped", body =>
    {
        ItemInstance? device = body.View?.Belongings.Equipped(SlotType.Device);
        return device != null && device.Type == ItemType.Phone;
    });

    public static readonly BotFact Fullscreen = new BotFact("fullscreen", "fullscreen", body =>
    {
        DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();
        return mode == DisplayServer.WindowMode.Fullscreen || mode == DisplayServer.WindowMode.ExclusiveFullscreen;
    });

    // Money in the pocket, at least this much.
    public static BotFact DollarsAtLeast(int dollars)
    {
        return new BotFact("money", "$" + dollars + " or more", body =>
        {
            ClientView? view = body.View;
            return view != null && view.Dollars >= dollars;
        });
    }

    // At least one of this item, loose in the bag or worn.
    public static BotFact Carrying(ItemType type)
    {
        return new BotFact("carry-" + type, "carrying a " + ItemCatalog.Get(type).Name, body =>
        {
            ClientView? view = body.View;

            if (view == null)
            {
                return false;
            }

            foreach (ItemStack stack in view.Belongings.Stacks.Stacks)
            {
                if (stack.Type == type && stack.Quantity > 0)
                {
                    return true;
                }
            }

            foreach (ItemInstance item in view.Belongings.Instances)
            {
                if (item.Type == type)
                {
                    return true;
                }
            }

            return false;
        });
    }
}
