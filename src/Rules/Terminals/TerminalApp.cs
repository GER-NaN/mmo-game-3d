namespace MmoGame3d.Rules.Terminals;

public class TerminalApp
{
    public TerminalApp(string id, string name, string lockedNotice)
    {
        Id = id;
        Name = name;
        LockedNotice = lockedNotice;
    }

    public string Id { get; }
    public string Name { get; }

    // Empty for an app that opens.
    public string LockedNotice { get; }

    public AppState State
    {
        get { return LockedNotice.Length == 0 ? AppState.Open : AppState.Locked; }
    }
}
