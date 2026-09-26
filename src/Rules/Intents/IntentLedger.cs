namespace MmoGame3d.Rules.Intents;

/// <summary>
/// The convention mmo-game settled for intents that spend: each carries an id, the
/// server acts on an id once, and it answers every id (approved, or refused with the
/// reason). A client resends an id it has no answer for, so a resent or doubled buy
/// gets the first answer again instead of buying twice.
///
/// One ledger per player. It keeps the last Capacity answers, which is far more than a
/// client ever has in flight.
/// </summary>
public class IntentLedger
{
    public const int Capacity = 64;

    private readonly Dictionary<uint, string> _answers = new Dictionary<uint, string>();
    private readonly Queue<uint> _order = new Queue<uint>();

    // The answer already given to this id: "" for approved, else the refusal. Null when
    // the id is new and must be acted on.
    public string? AnswerFor(uint intentId)
    {
        string? answer;
        return _answers.TryGetValue(intentId, out answer) ? answer : null;
    }

    public void Record(uint intentId, string answer)
    {
        if (_answers.ContainsKey(intentId))
        {
            return;
        }

        _answers[intentId] = answer;
        _order.Enqueue(intentId);

        while (_order.Count > Capacity)
        {
            _answers.Remove(_order.Dequeue());
        }
    }
}
