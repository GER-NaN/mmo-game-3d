namespace MmoGame3d.Server;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Data;
using MmoGame3d.Data.Progress;
using MmoGame3d.Networking;
using MmoGame3d.Rules.Achievements;

/// <summary>
/// Achievements on the server: the parts of the game that see one earned call Grant;
/// this keeps each once, tells the player, puts it on the status board and saves it.
/// </summary>
public class ServerAchievements
{
    private readonly ProgressNetwork _network;
    private readonly Network _session;
    private readonly PersistenceWorker _worker;
    private readonly AchievementStore _store;

    public ServerAchievements(ProgressNetwork network, Network session, PersistenceWorker worker, AchievementStore store)
    {
        _network = network;
        _session = session;
        _worker = worker;
        _store = store;
    }

    // What happens here, for the terminal's status board; set by ServerGame.
    public Action<string>? Post { get; set; }

    public void Grant(Session session, string id)
    {
        Achievement? achievement = Achievements.Find(id);

        if (achievement == null || session.Record == null || !session.Achievements.Add(id))
        {
            return;
        }

        Guid playerId = session.Record.PlayerId;
        _worker.Enqueue(() => _store.Grant(playerId, id), e => GD.PrintErr("Saving an achievement failed: " + e.Message));
        _session.SendNotice(session.PeerId, "Achievement: " + achievement.Title + ". " + achievement.Text);
        Post?.Invoke(session.Record.DisplayName + " earned \"" + achievement.Title + "\".");
        Send(session);
    }

    public void Send(Session session)
    {
        _network.SendAchievements(session.PeerId, new List<string>(session.Achievements).ToArray());
    }
}
