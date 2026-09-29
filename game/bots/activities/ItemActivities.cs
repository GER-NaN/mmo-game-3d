namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Chests;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Shops;
using MmoGame3d.Rules.World;
using MmoGame3d.Town;
using MmoGame3d.Ui;
using MmoGame3d.Vendors;
using MmoGame3d.Workbenches;

/// <summary>
/// Activities with things and money: picking up what lies about, the outskirts' chest,
/// the recycler (things for money), the electronics shop (money for things), the
/// workbench (a phone's battery), and dropping something. Each checks what the client
/// then holds, through its view of the player.
/// </summary>
public static class ItemActivities
{
    public static List<BotActivity> All()
    {
        return new List<BotActivity>
        {
            PickUp(),
            OpenChest(),
            Recycle(),
            Buy(ItemType.Battery),
            Buy(ItemType.EmpEmitter),
            Buy(ItemType.RamStick),
            SwapBattery(),
            DropSomething(),
        };
    }

    // Walks over the nearest thing on the ground in town, and sees the bag grow.
    private static BotActivity PickUp()
    {
        return new BotActivity("pick-up", new[] { BotFacts.Recyclable, BotFacts.Stack }, plan =>
        {
            int before = 0;

            return plan
                .InWorld()
                .GoTo(ZoneIds.Town)
                .Do("count the bag", body => before = BotFacts.BagCount(body))
                .PickUp()
                .Until("the bag holds more", body => BotFacts.BagCount(body) > before, 5);
        }).Says("Ooh, what's that?", "Finders keepers.");
    }

    // Opens the chest in the outskirts, if it holds something, and sees the bag grow.
    private static BotActivity OpenChest()
    {
        return new BotActivity("open-chest", new[] { BotFacts.Recyclable }, plan =>
        {
            int before = 0;

            return plan
                .InWorld()
                .GoTo(ZoneIds.Outskirts)
                .Do("count the bag", body => before = BotFacts.BagCount(body))
                .Use<Chest>()
                .Until("the bag holds more", body => BotFacts.BagCount(body) > before, 5);
        }).Says("Let's see what's in the chest.");
    }

    // One thing into the recycler, and the pocket fuller for it.
    private static BotActivity Recycle()
    {
        // It provides money: any need for an amount can be met by recycling, again and again.
        return new BotActivity("recycle", new[] { BotFacts.DollarsAtLeast(1) }, plan =>
        {
            int before = 0;

            return plan
                .InWorld()
                .Need(BotFacts.Recyclable)
                .GoTo(ZoneIds.Town)
                .Use<Recycler>()
                .UntilOpen("recycler")
                .Do("note the money", body => before = body.View!.Dollars)
                .Click("Recycle on something it can spare", body => BotScreens.RowButton(body.Find<RecyclerPanel>()?.GetNode("%Rows"), BotFacts.RecyclableRow(body) ?? "-", "Recycle"))
                .Until("paid for it", body => body.View!.Dollars > before, 5)
                .Press("ui_cancel")
                .UntilClosed("recycler");
        }).Says("Every bit helps.", "Turning junk into cash.");
    }

    // Buys one from the electronics shop, with the money for it earned first if short.
    private static BotActivity Buy(ItemType type)
    {
        ShopOffer? offer = OfferFor(type);
        int price = offer != null ? offer.Price : 0;
        BotFact carrying = BotFacts.Carrying(type);

        return new BotActivity("buy-" + type.ToString().ToLowerInvariant(), new[] { carrying }, plan =>
        {
            int before = 0;

            return plan
                .InWorld()
                .Need(BotFacts.DollarsAtLeast(price))
                .GoTo(ZoneIds.Shop)
                .Use<Vendor>()
                .UntilOpen("shop")
                .Do("count them", body => before = Count(body, type))
                .Click("Buy on " + type, body => BotScreens.RowButton(body.Find<ShopPanel>()?.GetNode("%Offers"), type + "_", "Buy"))
                .Until("one more " + type, body => Count(body, type) > before, 5)
                .Press("ui_cancel")
                .UntilClosed("shop");
        }).Says("Shopping time.", "How much for the battery?");
    }

    // A new battery into the phone at the shop's workbench, bought first if none is in the
    // bag: the old one out, the new one in, and the phone at full charge.
    private static BotActivity SwapBattery()
    {
        return new BotActivity("swap-battery", plan => plan
            .InWorld()
            .Need(BotFacts.Carrying(ItemType.Battery))
            .GoTo(ZoneIds.Shop)
            .Use<Workbench>()
            .UntilOpen("workbench")
            .Click("Take battery out", body => BotScreens.FirstButtonStarting(body.Find<WorkbenchPanel>(), "Take battery out"))
            .Click("Put in a new battery", body => BotScreens.FirstButtonStarting(body.Find<WorkbenchPanel>(), "Put in a new battery"))
            .Until("a phone at full charge", body => PhoneCharge(body) >= 0.99f, 5)
            .Press("ui_cancel")
            .UntilClosed("workbench"))
            .Says("Fresh battery.");
    }

    // The highest charge of a battery inside any phone the player has, 0 to 1; -1 for none.
    private static float PhoneCharge(BotBody body)
    {
        Client.ClientView? view = body.View;
        float best = -1;

        if (view == null)
        {
            return best;
        }

        foreach (ItemInstance phone in view.Belongings.Instances)
        {
            if (phone.Type != ItemType.Phone)
            {
                continue;
            }

            ItemInstance? battery = view.Belongings.Inside(phone, SlotType.Battery);

            if (battery != null && battery.Charge != null)
            {
                best = Godot.Mathf.Max(best, battery.Charge.Value);
            }
        }

        return best;
    }

    // Drops one stack from the bag, and sees the bag shrink.
    private static BotActivity DropSomething()
    {
        return new BotActivity("drop-something", plan =>
        {
            int before = 0;

            return plan
                .InWorld()
                .Need(BotFacts.Stack)
                .Do("count the bag", body => before = BotFacts.BagCount(body))
                .Press("inventory")
                .UntilOpen("inventory")
                .Click("the first Drop", body => BotScreens.RowButton(body.Find<InventoryPanel>()?.GetNode("%Items"), "Stack_", "Drop"))
                .Until("the bag holds less", body => BotFacts.BagCount(body) < before, 5)
                .Press("inventory")
                .UntilClosed("inventory");
        }).Says("Don't need this anymore.");
    }

    private static int Count(BotBody body, ItemType type)
    {
        int count = 0;
        Client.ClientView? view = body.View;

        if (view == null)
        {
            return 0;
        }

        foreach (ItemStack stack in view.Belongings.Stacks.Stacks)
        {
            if (stack.Type == type)
            {
                count += stack.Quantity;
            }
        }

        foreach (ItemInstance item in view.Belongings.Instances)
        {
            if (item.Type == type)
            {
                count++;
            }
        }

        return count;
    }

    private static ShopOffer? OfferFor(ItemType type)
    {
        foreach (ShopOffer offer in Shops.Offers(Shops.Electronics))
        {
            if (offer.Type == type)
            {
                return offer;
            }
        }

        return null;
    }
}
