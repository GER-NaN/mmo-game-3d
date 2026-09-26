namespace MmoGame3d.Rules.Players;

/// <summary>
/// A label to read on screen, not an identity: names are not unique, and a name is
/// chosen once, when the player is made.
/// </summary>
public static class DisplayName
{
    public const int MaxLength = 16;

    // Null when the name is fine, else the reason, worded for the player.
    public static string? Problem(string name)
    {
        string trimmed = name.Trim();

        if (trimmed.Length == 0)
        {
            return "Enter a name.";
        }

        if (trimmed.Length > MaxLength)
        {
            return "A name is at most " + MaxLength + " characters.";
        }

        foreach (char c in trimmed)
        {
            if (char.IsControl(c))
            {
                return "A name cannot hold control characters.";
            }
        }

        return null;
    }
}
