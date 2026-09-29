namespace MmoGame3d.Bots;

using System;

/// <summary>
/// Something a player can see about themselves, checked on the client: the phone is
/// equipped, $500 or more. The key says what it is about ("money"), so an activity that
/// provides money serves a need for any amount; the name says this one ("$500 or more").
/// Activities provide facts by key, and steps need them (NeedStep).
/// </summary>
public class BotFact
{
    private readonly Func<BotBody, bool> _check;

    public BotFact(string key, string name, Func<BotBody, bool> check)
    {
        Key = key;
        Name = name;
        _check = check;
    }

    public string Key { get; }

    public string Name { get; }

    public bool Holds(BotBody body)
    {
        return _check(body);
    }
}
