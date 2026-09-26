namespace MmoGame3d.Rules.Items;

/// <summary>
/// How a phone's battery runs down. Placeholder rates from mmo-game's phone design:
/// about three days to empty when only carried, about three hours while online on it.
/// </summary>
public static class Power
{
    public const float IdleDrainPerSecond = 1f / (3f * 24f * 3600f);
    public const float InUseDrainPerSecond = 1f / (3f * 3600f);

    public static float Drain(float charge, float seconds, bool inUse)
    {
        float rate = inUse ? InUseDrainPerSecond : IdleDrainPerSecond;
        return Math.Max(0f, charge - (rate * seconds));
    }

    // Null when the equipped phone can take the player online, else why not.
    public static string? CannotGoOnline(Belongings belongings)
    {
        if (belongings.Equipped(SlotType.Device) == null)
        {
            return "You have no phone equipped. Equip one from your inventory.";
        }

        ItemInstance? battery = belongings.DeviceBattery();

        if (battery == null)
        {
            return "Your phone has no battery. Put one in at a workbench.";
        }

        if (battery.Charge == null || battery.Charge <= 0f)
        {
            return "Your phone's battery is dead. Swap it at a workbench.";
        }

        return null;
    }

    // A whole percent, for showing and for deciding when a change is worth sending.
    public static int Percent(float? charge)
    {
        return charge == null ? 0 : (int)Math.Ceiling(charge.Value * 100f - 0.0001f);
    }
}
