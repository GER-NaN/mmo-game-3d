namespace MmoGame3d.Rules.Items;

// A place an item goes: a player's equipment slots, and the slots inside an item (a
// phone's battery). One mechanism at two levels, as in mmo-game.
public enum SlotType
{
    Device,
    Battery,
    Tool,
    Drone,
}
