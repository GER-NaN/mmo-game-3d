namespace MmoGame3d.Server;

using System;
using MmoGame3d.College;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Skills;

/// <summary>
/// The college: the registrar gives the Class (once, before the first career) and
/// enrolls a player in a career or changes it; a professor ranks a player up, at the
/// player's own college. Every request needs the right person in reach, since a client
/// could ask from anywhere.
/// </summary>
public class ServerCollege
{
    private const float ReachSlack = 1.5f;

    private readonly ProgressNetwork _network;
    private readonly Network _session;
    private readonly ServerProgress _progress;

    public ServerCollege(ProgressNetwork network, Network session, ServerProgress progress)
    {
        _network = network;
        _session = session;
        _progress = progress;
    }

    public void Talk(Session session, CollegePerson person)
    {
        session.OpenCollegePerson = person;
        _progress.Send(session);
        _network.SendCollegeOpened(session.PeerId, person.Role);
    }

    public void TakeClass(Session session)
    {
        if (!Near(session, CollegePerson.Registrar))
        {
            return;
        }

        PlayerCareer career = session.Progress.Career;

        if (career.ClassTaken)
        {
            return;
        }

        career.TakeClass(session.ZoneId!);
        _session.SendNotice(session.PeerId, "Class done. This is your college now: your ranks are taken here.");
        _progress.Changed(session);
    }

    public void Enroll(Session session, int careerId)
    {
        CareerDefinition? definition = CareerCatalog.Find(careerId);

        if (definition == null || !Near(session, CollegePerson.Registrar))
        {
            return;
        }

        CareerId? leaving = session.Progress.Career.Career;
        string? refusal = session.Progress.Career.Enroll(definition, session.Progress.Skills);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        session.RankReadyNoted = false;
        string left = leaving.HasValue ? " You left " + CareerCatalog.Get(leaving.Value).Name + " and its progress behind." : "";
        _session.SendNotice(session.PeerId, "You are now a " + definition.Name + " (Apprentice)." + left);
        _progress.Changed(session);
    }

    public void RankUp(Session session)
    {
        if (!Near(session, CollegePerson.Professor))
        {
            return;
        }

        string? refusal = session.Progress.Career.RankUp(session.ZoneId!);

        if (refusal != null)
        {
            _session.SendNotice(session.PeerId, refusal);
            return;
        }

        PlayerCareer career = session.Progress.Career;
        session.RankReadyNoted = false;
        _session.SendNotice(session.PeerId, "Your professor signs it off: you are " + CareerCatalog.Title(career.Career, career.Rank) + ".");
        _progress.Changed(session);
    }

    private bool Near(Session session, string role)
    {
        CollegePerson? person = session.OpenCollegePerson;
        bool near = person != null && Godot.GodotObject.IsInstanceValid(person) && person.Role == role
            && session.Body != null && person.IsInReach(session.Body.GlobalPosition, ReachSlack);

        if (!near)
        {
            _session.SendNotice(session.PeerId, "Talk to the " + role + " at the college for that.");
        }

        return near;
    }
}
