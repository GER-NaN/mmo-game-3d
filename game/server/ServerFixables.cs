namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.Social;
using MmoGame3d.Town;

/// <summary>
/// Small things in town break now and then, and anyone can fix one: the act that earns
/// Field repair. One breaks at the start, so there is something to find, then another
/// every so often while fewer than MaxBroken are broken.
/// </summary>
public class ServerFixables
{
    // Placeholders.
    private const double BreakEverySeconds = 90;
    private const int MaxBroken = 2;

    private readonly List<Fixable> _all;
    private readonly Network _session;
    private readonly ServerProgress _progress;
    private readonly Random _random = new Random();
    private double _sinceBreak = BreakEverySeconds;

    public ServerFixables(List<Fixable> all, Network session, ServerProgress progress)
    {
        _all = all;
        _session = session;
        _progress = progress;
    }

    public void Fix(Session session, Fixable fixable)
    {
        if (!fixable.Broken)
        {
            return;
        }

        fixable.Broken = false;
        session.Body?.Show(Gestures.Repair);
        _session.SendNotice(session.PeerId, "You fixed the " + fixable.FixableName + ".");
        _progress.Award(session, SkillId.FieldRepair, SkillAwards.FieldRepairPerFix);
    }

    public void Tick(double delta)
    {
        _sinceBreak += delta;

        if (_sinceBreak < BreakEverySeconds)
        {
            return;
        }

        _sinceBreak = 0;
        List<Fixable> whole = new List<Fixable>();
        int broken = 0;

        foreach (Fixable fixable in _all)
        {
            if (fixable.Broken)
            {
                broken++;
            }
            else
            {
                whole.Add(fixable);
            }
        }

        if (broken < MaxBroken && whole.Count > 0)
        {
            Fixable breaking = whole[_random.Next(whole.Count)];
            breaking.Broken = true;
            Godot.GD.Print("The " + breaking.FixableName + " (" + breaking.Name + ") broke");
        }
    }
}
