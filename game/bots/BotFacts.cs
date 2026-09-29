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

    // Something in the bag the recycler takes: a stack, or a thing not worn.
    public static readonly BotFact Recyclable = new BotFact("recyclable", "something to recycle", body => BagCount(body) > 0);

    // A stack in the bag, which is what can be dropped.
    public static readonly BotFact Stack = new BotFact("stack", "a stack in the bag", body =>
    {
        ClientView? view = body.View;
        return view != null && view.Belongings.Stacks.Stacks.Count > 0;
    });

    // How many things are in the bag, worn things left out: each of a stack, each loose
    // thing.
    public static int BagCount(BotBody body)
    {
        ClientView? view = body.View;

        if (view == null)
        {
            return 0;
        }

        int count = 0;

        foreach (ItemStack stack in view.Belongings.Stacks.Stacks)
        {
            count += stack.Quantity;
        }

        foreach (ItemInstance item in view.Belongings.Instances)
        {
            if (item.IsLoose)
            {
                count++;
            }
        }

        return count;
    }

    // Money in the pocket, at least this much.
    public static BotFact DollarsAtLeast(int dollars)
    {
        return new BotFact("money", "$" + dollars + " or more", body =>
        {
            ClientView? view = body.View;
            return view != null && view.Dollars >= dollars;
        });
    }

    // At least one of this item in the bag: in a stack, or a thing loose there (not worn,
    // not inside another thing, as a phone's battery is).
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
                if (item.Type == type && item.IsLoose)
                {
                    return true;
                }
            }

            return false;
        });
    }
}
