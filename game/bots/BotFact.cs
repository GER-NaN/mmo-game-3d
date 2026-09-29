namespace MmoGame3d.Bots;

using System;

/// <summary>
/// Something a player can see about themselves, checked on the client: the phone is
/// equipped, $500 or more. Activities provide facts, and steps need them (NeedStep).
/// </summary>
public class BotFact
{
    private readonly Func<BotBody, bool> _check;

    public BotFact(string name, Func<BotBody, bool> check)
    {
        Name = name;
        _check = check;
    }

    public string Name { get; }

    public bool Holds(BotBody body)
    {
        return _check(body);
    }
}
