namespace MmoGame3d.Dev;

using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Skills;

/// <summary>
/// Something true or not about the bot, as its player would see it: where it is, its money,
/// what is in its bag or worn, its phone's charge, its career. Activities need facts and give
/// them; a goal is the facts it wants (BotResolver). A promise answers a need of the same
/// kind and key (money is money, whatever the amount; the resolver looks again after).
/// </summary>
public sealed class BotFact
{
    private BotFact(FactKind kind, string key, int amount)
    {
        Kind = kind;
        Key = key;
        Amount = amount;
    }

    public FactKind Kind { get; }

    // The zone, the item type or the career; "" when the kind has none.
    public string Key { get; }

    public int Amount { get; }

    public static BotFact InZone(string zoneId)
    {
        return new BotFact(FactKind.InZone, zoneId, 0);
    }

    public static BotFact MoneyAtLeast(int dollars)
    {
        return new BotFact(FactKind.MoneyAtLeast, "", dollars);
    }

    public static BotFact Has(ItemType type)
    {
        return new BotFact(FactKind.Has, type.ToString(), (int)type);
    }

    public static BotFact Wears(ItemType type)
    {
        return new BotFact(FactKind.Wears, type.ToString(), (int)type);
    }

    public static BotFact PhoneAtLeast(int percent)
    {
        return new BotFact(FactKind.PhoneAtLeast, "", percent);
    }

    public static BotFact Career(CareerId career)
    {
        return new BotFact(FactKind.Career, career.ToString(), (int)career);
    }

    public static BotFact HasSomethingToSell()
    {
        return new BotFact(FactKind.HasSomethingToSell, "", 0);
    }

    public static BotFact Offline()
    {
        return new BotFact(FactKind.Offline, "", 0);
    }

    public bool IsTrue(BotBody body)
    {
        switch (Kind)
        {
            case FactKind.InZone:
                return body.ZoneId == Key;
            case FactKind.MoneyAtLeast:
                return body.Money >= Amount;
            case FactKind.Has:
                return body.Has((ItemType)Amount);
            case FactKind.Wears:
                return body.Wears((ItemType)Amount);
            case FactKind.PhoneAtLeast:
                return body.PhonePercent >= Amount;
            case FactKind.Career:
                return body.Career == Amount;
            case FactKind.HasSomethingToSell:
                return body.HasSomethingToSell;
            case FactKind.Offline:
                return !body.IsOnline;
            default:
                return false;
        }
    }

    // Whether a promise of this fact meets a need of that one.
    public bool Answers(BotFact need)
    {
        return Kind == need.Kind && Key == need.Key;
    }

    public override string ToString()
    {
        switch (Kind)
        {
            case FactKind.InZone:
                return "in " + Key;
            case FactKind.MoneyAtLeast:
                return "$" + Amount + " or more";
            case FactKind.Has:
                return "a " + Key + " in the bag";
            case FactKind.Wears:
                return "a " + Key + " worn";
            case FactKind.PhoneAtLeast:
                return "phone at " + Amount + "% or more";
            case FactKind.Career:
                return "a " + Key;
            case FactKind.HasSomethingToSell:
                return "something to sell";
            case FactKind.Offline:
                return "offline";
            default:
                return Kind.ToString();
        }
    }
}
