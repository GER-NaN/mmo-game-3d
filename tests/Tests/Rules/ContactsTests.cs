namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Social;

public class ContactsTests
{
    private static readonly Guid Self = Guid.NewGuid();

    [Fact]
    public void AFriendIsAddedOnceAndNeverYourself()
    {
        Contacts contacts = new Contacts();
        Guid bea = Guid.NewGuid();

        Assert.Null(contacts.Befriend(Self, bea, "Bea"));
        Assert.True(contacts.IsFriend(bea));
        Assert.NotNull(contacts.Befriend(Self, bea, "Bea"));
        Assert.NotNull(contacts.Befriend(Self, Self, "Me"));
    }

    [Fact]
    public void NobodyIsBothAFriendAndIgnored()
    {
        Contacts contacts = new Contacts();
        Guid bea = Guid.NewGuid();
        contacts.Befriend(Self, bea, "Bea");

        Assert.Null(contacts.Ignore(Self, bea, "Bea"));
        Assert.False(contacts.IsFriend(bea));
        Assert.True(contacts.Ignores(bea));

        Assert.Null(contacts.Befriend(Self, bea, "Bea"));
        Assert.False(contacts.Ignores(bea));
    }

    [Fact]
    public void RemovingTakesThemOffEitherList()
    {
        Contacts contacts = new Contacts();
        Guid bea = Guid.NewGuid();
        contacts.Ignore(Self, bea, "Bea");

        Assert.True(contacts.Remove(bea));
        Assert.False(contacts.Ignores(bea));
        Assert.False(contacts.Remove(bea));
    }
}
