namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Terminals;

/// <summary>
/// The code cracker at public terminals: the Hacking skill's act. The code lives here;
/// the client sends guesses and gets the answers. A Computer Scientist also sees which
/// places were right, the career's "sees more in the terminal".
/// </summary>
public class ServerHacking
{
    private const int Playing = 0;
    private const int Cracked = 1;
    private const int LockedOut = 2;

    private readonly TerminalNetwork _network;
    private readonly Network _session;
    private readonly ServerTerminals _terminals;
    private readonly ServerProgress _progress;
    private readonly Dictionary<Session, CodeCracker> _codes = new Dictionary<Session, CodeCracker>();
    private readonly Random _random = new Random();

    public ServerHacking(TerminalNetwork network, Network session, ServerTerminals terminals, ServerProgress progress)
    {
        _network = network;
        _session = session;
        _terminals = terminals;
        _progress = progress;
    }

    public void Start(Session session)
    {
        if (!AtPublicTerminal(session))
        {
            return;
        }

        _codes[session] = new CodeCracker(_random);
        Send(session, _codes[session]);
    }

    public void Guess(Session session, string guess)
    {
        CodeCracker? code;

        if (!AtPublicTerminal(session) || !_codes.TryGetValue(session, out code))
        {
            return;
        }

        string? refusal = code.Guess(guess);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        if (code.Solved)
        {
            _session.SendNotice(session.PeerId, "Code cracked.");
            _progress.Award(session, SkillId.Hacking, SkillAwards.HackingPerCode);
        }

        Send(session, code);
    }

    public void Forget(Session session)
    {
        _codes.Remove(session);
    }

    private bool AtPublicTerminal(Session session)
    {
        bool at = session.Body != null && session.Body.IsOnline && !_terminals.IsOnPhone(session);

        if (!at)
        {
            _session.SendNotice(session.PeerId, "Codes are cracked at public terminals.");
        }

        return at;
    }

    private void Send(Session session, CodeCracker code)
    {
        bool scientist = session.Progress.Career.Career == CareerId.ComputerScientist;
        string[] positions = new string[code.Positions.Count];

        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = scientist ? code.Positions[i] : "";
        }

        int status = code.Solved ? Cracked : (code.Over ? LockedOut : Playing);
        _network.SendCrack(session.PeerId, new List<string>(code.Guesses).ToArray(), new List<int>(code.Exact).ToArray(), new List<int>(code.Partial).ToArray(), positions, code.GuessesLeft, status);
    }
}
