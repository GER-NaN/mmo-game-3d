namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Intents;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Shops;

public class ShopTests
{
    [Fact]
    public void ANewPlayerCanAffordTheBatteryAndNotThePhone()
    {
        IReadOnlyList<ShopOffer> offers = Shops.Offers(Shops.Electronics);
        ShopOffer battery = offers.First(offer => offer.Type == ItemType.Battery);
        ShopOffer phone = offers.First(offer => offer.Type == ItemType.Phone);

        Assert.Null(Shops.CannotBuy(Shops.StartingDollars, battery));
        Assert.NotNull(Shops.CannotBuy(Shops.StartingDollars, phone));
    }

    [Fact]
    public void AnUnknownOfferIsNothing()
    {
        Assert.Null(Shops.Offer(Shops.Electronics, 99));
        Assert.Null(Shops.Offer("nowhere", 0));
    }

    [Fact]
    public void ARepeatedIntentGetsTheFirstAnswer()
    {
        IntentLedger ledger = new IntentLedger();

        Assert.Null(ledger.AnswerFor(7));
        ledger.Record(7, "");
        ledger.Record(7, "a later answer");

        Assert.Equal("", ledger.AnswerFor(7));
    }

    [Fact]
    public void TheLedgerForgetsTheOldestPastItsCapacity()
    {
        IntentLedger ledger = new IntentLedger();

        for (uint id = 0; id <= IntentLedger.Capacity; id++)
        {
            ledger.Record(id, "");
        }

        Assert.Null(ledger.AnswerFor(0));
        Assert.NotNull(ledger.AnswerFor(IntentLedger.Capacity));
    }
}
