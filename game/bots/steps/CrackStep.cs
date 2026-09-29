namespace MmoGame3d.Bots;

using System.Collections.Generic;
using Godot;
using MmoGame3d.Ui;

/// <summary>
/// Plays the terminal's code cracker (the "New code" already pressed): four digits 0 to
/// 5, and after each guess how many are in place and how many elsewhere. It reads the
/// answers off the screen and guesses a code that agrees with all of them, at random among
/// those that do, as a thinking player would. Done when the screen says "Cracked." or
/// "Locked out."; which, it records.
/// </summary>
public class CrackStep : BotStep
{
    private const double LookInterval = 0.3;
    private const int Digits = 4;
    private const int Values = 6;

    private double _sinceLook;
    private int _guessed;

    public CrackStep()
        : base("crack the code", 90)
    {
    }

    public override BotIntent Intent
    {
        get { return BotIntent.Screen; }
    }

    public override BotStepState Tick(BotBody body, double delta)
    {
        _sinceLook += delta;

        if (_sinceLook < LookInterval)
        {
            return BotStepState.Running;
        }

        _sinceLook = 0;
        TerminalScreen? screen = body.Find<TerminalScreen>();

        // Offline before it was cracked: fainted, or thrown off the terminal.
        if (screen == null)
        {
            return Fail("the terminal closed before the code was cracked");
        }

        List<string> lines = new List<string>();
        CollectLabels(screen, lines);

        foreach (string line in lines)
        {
            if (line == "Cracked." || line.StartsWith("Locked out."))
            {
                body.Events.Write("cracker", line + " after " + _guessed + " guesses");
                return BotStepState.Done;
            }
        }

        List<int[]> answers = Answers(lines);

        // A guess is answered before the next; the field has the keys while guessing.
        if (answers.Count < _guessed || body.FocusedField() == null)
        {
            return BotStepState.Running;
        }

        string guess = NextGuess(answers, body);
        _guessed++;
        body.Type(guess);
        return BotStepState.Running;
    }

    // The screen's lines of the form "0123   in place 1   elsewhere 2", as guess digits
    // then the two counts.
    private static List<int[]> Answers(List<string> lines)
    {
        List<int[]> answers = new List<int[]>();

        foreach (string line in lines)
        {
            string[] words = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 6 || words[0].Length != Digits || words[1] != "in" || words[2] != "place" || words[4] != "elsewhere")
            {
                continue;
            }

            int exact;
            int partial;

            if (!int.TryParse(words[3], out exact) || !int.TryParse(words[5], out partial))
            {
                continue;
            }

            int[] answer = new int[Digits + 2];

            for (int i = 0; i < Digits; i++)
            {
                answer[i] = words[0][i] - '0';
            }

            answer[Digits] = exact;
            answer[Digits + 1] = partial;
            answers.Add(answer);
        }

        return answers;
    }

    // A code agreeing with every answer so far, at random among those that do.
    private static string NextGuess(List<int[]> answers, BotBody body)
    {
        List<int[]> fits = new List<int[]>();
        int[] code = new int[Digits];

        for (int n = 0; n < 1296; n++)
        {
            int rest = n;

            for (int i = 0; i < Digits; i++)
            {
                code[i] = rest % Values;
                rest /= Values;
            }

            if (Agrees(code, answers))
            {
                fits.Add((int[])code.Clone());
            }
        }

        int[] pick = fits.Count > 0 ? fits[body.Random.Next(fits.Count)] : code;
        return string.Concat(pick[0], pick[1], pick[2], pick[3]);
    }

    private static bool Agrees(int[] code, List<int[]> answers)
    {
        foreach (int[] answer in answers)
        {
            int exact = 0;
            int[] inCode = new int[Values];
            int[] inGuess = new int[Values];

            for (int i = 0; i < Digits; i++)
            {
                if (code[i] == answer[i])
                {
                    exact++;
                }

                inCode[code[i]]++;
                inGuess[answer[i]]++;
            }

            int shared = 0;

            for (int v = 0; v < Values; v++)
            {
                shared += Mathf.Min(inCode[v], inGuess[v]);
            }

            if (exact != answer[Digits] || shared - exact != answer[Digits + 1])
            {
                return false;
            }
        }

        return true;
    }

    private static void CollectLabels(Node node, List<string> lines)
    {
        foreach (Node child in node.GetChildren())
        {
            Label? label = child as Label;

            if (label != null && label.IsVisibleInTree())
            {
                lines.Add(label.Text.Trim());
            }

            CollectLabels(child, lines);
        }
    }
}
