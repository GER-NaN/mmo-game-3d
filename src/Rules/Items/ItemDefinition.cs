namespace MmoGame3d.Rules.Items;

public class ItemDefinition
{
    public ItemDefinition(ItemType type, string name, string description, bool stackable)
    {
        Type = type;
        Name = name;
        Description = description;
        Stackable = stackable;
    }

    public ItemType Type { get; }
    public string Name { get; }
    public string Description { get; }

    // A stack is a count of identical things; a thing that is not stackable has an
    // identity and state of its own (this phone), and comes with equipment.
    public bool Stackable { get; }
}
