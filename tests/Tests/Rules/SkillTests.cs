namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Skills;

public class SkillTests
{
    [Fact]
    public void ExperienceRaisesTheLevelAndSaysWhenItDid()
    {
        SkillBook skills = new SkillBook();

        Assert.Equal(1, skills.Level(SkillId.Hacking));
        Assert.Equal(0, skills.Add(SkillId.Hacking, SkillCatalog.XpForLevel(2) - 1));
        Assert.Equal(2, skills.Add(SkillId.Hacking, 1));
        Assert.Equal(2, skills.Level(SkillId.Hacking));
    }

    [Fact]
    public void AgilityPaysForEveryFullStretchAndEveryFewJumps()
    {
        AgilityCounter agility = new AgilityCounter();

        Assert.Equal(0, agility.Walked(SkillAwards.AgilityMetresPerXp - 1f));
        Assert.Equal(1, agility.Walked(1f));

        long fromJumps = 0;

        for (int i = 0; i < SkillAwards.AgilityJumpsPerXp; i++)
        {
            fromJumps += agility.Jumped();
        }

        Assert.Equal(1, fromJumps);
    }
}
