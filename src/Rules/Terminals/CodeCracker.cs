namespace MmoGame3d.Rules.Terminals;

/// <summary>
/// The Hacking skill's act: crack a short code, Mastermind style. The code is four
/// digits from 0 to 5; each guess is answered with how many digits are right and in
/// place, and how many are right but elsewhere. Kept on the server; the client only
/// ever sees the answers. The sizes are placeholders.
/// </summary>
public class CodeCracker
{
    public const int Length = 4;
    public const int Digits = 6;
    public const int MaxGuesses = 8;

    private readonly int[] _code = new int[Length];
    private readonly List<string> _guesses = new List<string>();
    private readonly List<int> _exact = new List<int>();
    private readonly List<int> _partial = new List<int>();
    private readonly List<string> _positions = new List<string>();

    public CodeCracker(Random random)
    {
        for (int i = 0; i < Length; i++)
        {
            _code[i] = random.Next(Digits);
        }
    }

    // For tests: a known code.
    public CodeCracker(string code)
    {
        for (int i = 0; i < Length; i++)
        {
            _code[i] = code[i] - '0';
        }
    }

    public IReadOnlyList<string> Guesses
    {
        get { return _guesses; }
    }

    public IReadOnlyList<int> Exact
    {
        get { return _exact; }
    }

    public IReadOnlyList<int> Partial
    {
        get { return _partial; }
    }

    // Per guess, which places were right: "x..x". A Computer Scientist sees these.
    public IReadOnlyList<string> Positions
    {
        get { return _positions; }
    }

    public bool Solved { get; private set; }

    public bool Over
    {
        get { return Solved || _guesses.Count >= MaxGuesses; }
    }

    public int GuessesLeft
    {
        get { return MaxGuesses - _guesses.Count; }
    }

    // Null when the guess was taken, or why not.
    public string? Guess(string guess)
    {
        if (Over)
        {
            return "This code is finished. Start a new one.";
        }

        guess = guess.Trim();

        if (guess.Length != Length)
        {
            return "A guess is " + Length + " digits.";
        }

        int[] digits = new int[Length];

        for (int i = 0; i < Length; i++)
        {
            int digit = guess[i] - '0';

            if (digit < 0 || digit >= Digits)
            {
                return "Use the digits 0 to " + (Digits - 1) + ".";
            }

            digits[i] = digit;
        }

        int exact = 0;
        char[] places = new char[Length];
        int[] codeLeft = new int[Digits];
        int[] guessLeft = new int[Digits];

        for (int i = 0; i < Length; i++)
        {
            if (digits[i] == _code[i])
            {
                exact++;
                places[i] = 'x';
            }
            else
            {
                places[i] = '.';
                codeLeft[_code[i]]++;
                guessLeft[digits[i]]++;
            }
        }

        int partial = 0;

        for (int digit = 0; digit < Digits; digit++)
        {
            partial += Math.Min(codeLeft[digit], guessLeft[digit]);
        }

        _guesses.Add(guess);
        _exact.Add(exact);
        _partial.Add(partial);
        _positions.Add(new string(places));
        Solved = exact == Length;
        return null;
    }
}
