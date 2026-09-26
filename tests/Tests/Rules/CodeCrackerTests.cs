namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Terminals;

public class CodeCrackerTests
{
    [Fact]
    public void AGuessIsAnsweredWithRightPlacesAndRightDigits()
    {
        CodeCracker cracker = new CodeCracker("1123");

        Assert.Null(cracker.Guess("1311"));

        // The first 1 is in place; the 3 and one more 1 are in the code elsewhere.
        Assert.Equal(1, cracker.Exact[0]);
        Assert.Equal(2, cracker.Partial[0]);
        Assert.Equal("x...", cracker.Positions[0]);
    }

    [Fact]
    public void TheRightCodeSolvesItAndEndsIt()
    {
        CodeCracker cracker = new CodeCracker("5043");

        Assert.Null(cracker.Guess("5043"));
        Assert.True(cracker.Solved);
        Assert.NotNull(cracker.Guess("0000"));
    }

    [Fact]
    public void BadGuessesAreRefusedAndEightWrongOnesEndIt()
    {
        CodeCracker cracker = new CodeCracker("0000");

        Assert.NotNull(cracker.Guess("12"));
        Assert.NotNull(cracker.Guess("1239"));

        for (int i = 0; i < CodeCracker.MaxGuesses; i++)
        {
            Assert.Null(cracker.Guess("1111"));
        }

        Assert.True(cracker.Over);
        Assert.False(cracker.Solved);
    }

    [Fact]
    public void AResultReadsInGuessesAndMinutes()
    {
        Assert.Equal("3 guesses, 1:05", Leaderboards.Result(Leaderboards.CodeCracker, 3, 64.6));
        Assert.Equal("1 guess, 0:07", Leaderboards.Result(Leaderboards.CodeCracker, 1, 7));
        Assert.True(Leaderboards.LowerIsBetter(Leaderboards.CodeCracker));
    }
}
