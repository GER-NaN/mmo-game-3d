namespace MmoGame3d.Rules.Items;

/// <summary>
/// Things changing hands: dropped on the ground, or given to another player. Only stacks
/// and pocket change for now; a phone and its battery stay with their owner. Each change
/// returns null when it happened, or the reason it did not, worded for the player, and
/// either all of it happens or none.
/// </summary>
public static class Handover
{
    // How close two players must be to hand something over, in world units.
    public const float GiveReach = 5f;

    public static string? Take(Inventory from, ItemType type, ItemTier tier, int quantity)
    {
        if (quantity <= 0)
        {
            return "Choose how many.";
        }

        if (!from.TryRemove(type, tier, quantity))
        {
            return "You do not have " + quantity + " " + ItemCatalog.Describe(type, tier) + ".";
        }

        return null;
    }

    public static string? Give(Inventory from, Inventory to, ItemType type, ItemTier tier, int quantity)
    {
        string? refusal = Take(from, type, tier, quantity);

        if (refusal == null)
        {
            to.Add(type, tier, quantity);
        }

        return refusal;
    }

    // Dollars: the new amounts come back through the out parameters.
    public static string? GiveDollars(int fromDollars, int toDollars, int amount, out int fromAfter, out int toAfter)
    {
        fromAfter = fromDollars;
        toAfter = toDollars;

        if (amount <= 0)
        {
            return "Choose how much.";
        }

        if (amount > fromDollars)
        {
            return "You have only $" + fromDollars + ".";
        }

        fromAfter = fromDollars - amount;
        toAfter = toDollars + amount;
        return null;
    }
}
