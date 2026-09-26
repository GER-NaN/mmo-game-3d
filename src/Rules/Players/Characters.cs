namespace MmoGame3d.Rules.Players;

/// <summary>
/// An account's characters. world.md: an account has two characters, and more can be
/// purchased; the purchase is not built, so the count is fixed here.
/// </summary>
public static class Characters
{
    public const int SlotsPerAccount = 2;

    // The first free slot, or -1 when every slot is taken.
    public static int FreeSlot(IEnumerable<int> taken)
    {
        HashSet<int> used = new HashSet<int>(taken);

        for (int slot = 0; slot < SlotsPerAccount; slot++)
        {
            if (!used.Contains(slot))
            {
                return slot;
            }
        }

        return -1;
    }
}
