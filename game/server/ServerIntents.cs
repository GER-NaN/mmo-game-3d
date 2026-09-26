namespace MmoGame3d.Server;

using System;
using MmoGame3d.Networking;

/// <summary>
/// The convention for intents that spend or give away: an intent carries an id, is acted
/// on once, and every id is answered ("" approved, else the refusal). A resent id gets
/// its first answer again, so a double click or a resend never spends twice.
/// </summary>
public class ServerIntents
{
    private readonly Network _network;

    public ServerIntents(Network network)
    {
        _network = network;
    }

    // act does the work and returns "" or the refusal; it runs at most once per id.
    public void Run(Session session, uint intentId, Func<string> act)
    {
        string? earlier = session.Intents.AnswerFor(intentId);

        if (earlier != null)
        {
            _network.SendIntentAnswer(session.PeerId, intentId, earlier);
            return;
        }

        string answer = act();
        session.Intents.Record(intentId, answer);
        _network.SendIntentAnswer(session.PeerId, intentId, answer);
    }
}
