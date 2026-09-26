namespace MmoGame3d.Rules.Terminals;

/// <summary>
/// Who is online through which fixed terminal. The rules, kept from mmo-game: one
/// player per terminal, one terminal per player, a disabled terminal cannot be used,
/// and being online does not survive a disconnect (a body with no client cannot sit at
/// a terminal, and one it held would stay taken forever). Reach is the caller's check,
/// since this knows nothing about space.
/// </summary>
public class TerminalAccess
{
    private readonly Dictionary<string, Guid> _playerByTerminal = new Dictionary<string, Guid>();
    private readonly Dictionary<Guid, string> _terminalByPlayer = new Dictionary<Guid, string>();

    // Null when the player may go online there, else the reason, worded for the player.
    public string? Use(Guid playerId, string terminalKey, bool enabled)
    {
        if (!enabled)
        {
            return "This terminal is not working.";
        }

        if (_terminalByPlayer.ContainsKey(playerId))
        {
            return "You are already online.";
        }

        if (_playerByTerminal.ContainsKey(terminalKey))
        {
            return "Someone is using this terminal.";
        }

        _playerByTerminal[terminalKey] = playerId;
        _terminalByPlayer[playerId] = terminalKey;
        return null;
    }

    // Goes offline; also what a disconnect calls. Returns the terminal freed, if any.
    public string? Leave(Guid playerId)
    {
        string? terminalKey;

        if (!_terminalByPlayer.TryGetValue(playerId, out terminalKey))
        {
            return null;
        }

        _terminalByPlayer.Remove(playerId);
        _playerByTerminal.Remove(terminalKey);
        return terminalKey;
    }

    public bool IsOnline(Guid playerId)
    {
        return _terminalByPlayer.ContainsKey(playerId);
    }

    public bool IsTaken(string terminalKey)
    {
        return _playerByTerminal.ContainsKey(terminalKey);
    }
}
