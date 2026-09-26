namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Achievements;

public class AchievementTests
{
    [Fact]
    public void AMapIsExploredAtNinetyFivePercent()
    {
        Assert.False(Achievements.IsExplored(94, 100));
        Assert.True(Achievements.IsExplored(95, 100));
        Assert.False(Achievements.IsExplored(0, 0));
    }

    [Fact]
    public void EveryAchievementCanBeFoundByItsId()
    {
        foreach (Achievement achievement in Achievements.All)
        {
            Assert.Same(achievement, Achievements.Find(achievement.Id));
        }

        Assert.Null(Achievements.Find("nope"));
    }
}
