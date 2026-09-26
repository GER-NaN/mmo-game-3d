namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Terminals;

public class AgentDefenseTests
{
    [Fact]
    public void TheSameSeedGivesTheSameChart()
    {
        List<DefenseCue> one = AgentDefense.Chart(42);
        List<DefenseCue> two = AgentDefense.Chart(42);

        Assert.True(one.Count > 50);
        Assert.Equal(one.Select(c => c.AtMs * 10 + c.Lane), two.Select(c => c.AtMs * 10 + c.Lane));
    }

    [Fact]
    public void PlayingEveryCueOnTheBeatIsAllPerfectWithTheCombo()
    {
        List<DefenseCue> chart = AgentDefense.Chart(7);
        List<DefensePress> presses = chart.Select(c => new DefensePress(c.AtMs, c.Lane)).ToList();

        DefenseResult result = AgentDefense.Score(chart, presses);

        Assert.Equal(chart.Count, result.Perfect);
        Assert.Equal(0, result.Missed);
        Assert.Equal(chart.Count, result.BestCombo);
        Assert.True(result.Points > chart.Count * AgentDefense.PerfectPoints);
    }

    [Fact]
    public void LatePressesAreGoodAndWrongLanesScoreNothing()
    {
        List<DefenseCue> chart = new List<DefenseCue> { new DefenseCue(1000, 0), new DefenseCue(2000, 1), new DefenseCue(3000, 2) };
        List<DefensePress> presses = new List<DefensePress>
        {
            new DefensePress(1100, 0),
            new DefensePress(2000, 3),
            new DefensePress(3500, 2),
        };

        DefenseResult result = AgentDefense.Score(chart, presses);

        Assert.Equal(0, result.Perfect);
        Assert.Equal(1, result.Good);
        Assert.Equal(2, result.Missed);
        Assert.Equal(AgentDefense.GoodPoints, result.Points);
    }

    [Fact]
    public void PressesCrossTheWireIntact()
    {
        int[] packed = { AgentDefense.Pack(new DefensePress(12345, 3)) };

        DefensePress back = AgentDefense.Unpack(packed)[0];

        Assert.Equal(12345, back.AtMs);
        Assert.Equal(3, back.Lane);
    }
}
