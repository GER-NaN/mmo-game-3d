namespace MmoGame3d.Tests.Data;
// Test classes that share the database run one at a time, so one class's wipe never
// lands in the middle of another's test.
[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<TestDatabase>
{
    public const string Name = "Database";
}
