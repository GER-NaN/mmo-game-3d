namespace MmoGame3d.Rules.Chat;

/// <summary>
/// How much one player may say: at most MaxLines lines in any window of WindowSeconds.
/// One per player, on the server. Time is passed in, so a test can move it.
/// </summary>
public class ChatRateLimit
{
    public const int MaxLines = 5;
    public const double WindowSeconds = 10;

    private readonly Queue<double> _sentAt = new Queue<double>();

    // True, and the line counted, when it may go out now.
    public bool TryTake(double nowSeconds)
    {
        while (_sentAt.Count > 0 && nowSeconds - _sentAt.Peek() >= WindowSeconds)
        {
            _sentAt.Dequeue();
        }

        if (_sentAt.Count >= MaxLines)
        {
            return false;
        }

        _sentAt.Enqueue(nowSeconds);
        return true;
    }
}
