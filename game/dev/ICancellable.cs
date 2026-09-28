namespace MmoGame3d.Dev;
/// <summary>
/// Stopped mid-way, by the interrupt roller or a lost connection: let go of held keys and
/// leave every screen as it is, the state a distracted player leaves. Tidying up is the
/// normal end of an activity, or the next one's business, never a cancel's.
/// </summary>
public interface ICancellable
{
    void Cancel(BotBody body, string reason);
}
