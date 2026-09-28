namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Skills;

public class CareerTests
{
    private static readonly CareerDefinition Engineer = CareerCatalog.Get(CareerId.MechanicalEngineer);

    [Fact]
    public void ACareerNeedsTheClassAndItsGate()
    {
        PlayerCareer career = new PlayerCareer();
        SkillBook skills = new SkillBook();

        Assert.NotNull(career.Enroll(Engineer, skills));

        career.TakeClass("college");
        Assert.NotNull(career.Enroll(Engineer, skills));

        MeetGate(skills, Engineer);
        Assert.Null(career.Enroll(Engineer, skills));
        Assert.Equal(CareerId.MechanicalEngineer, career.Career);
        Assert.Equal(CareerRank.Apprentice, career.Rank);
    }

    [Fact]
    public void OnlySupportingSkillsFeedTheCareer()
    {
        PlayerCareer career = Started(Engineer);

        career.AddSkillXp(SkillId.Workbench, 10);
        career.AddSkillXp(SkillId.Hacking, 10);

        Assert.Equal(10, career.Xp);
    }

    [Fact]
    public void ARankIsTakenAtYourOwnCollegeOnceTheExperienceIsThere()
    {
        PlayerCareer career = Started(Engineer);

        Assert.NotNull(career.RankUp("college"));

        career.AddCareerXp(CareerCatalog.XpForRank(CareerRank.Graduate));
        Assert.NotNull(career.RankUp("another-college"));
        Assert.Null(career.RankUp("college"));
        Assert.Equal(CareerRank.Graduate, career.Rank);
    }

    [Fact]
    public void ChangingCareerLosesItsProgressButNotTheSkills()
    {
        SkillBook skills = new SkillBook();
        PlayerCareer career = Started(Engineer, skills);
        career.AddCareerXp(CareerCatalog.XpForRank(CareerRank.Graduate));
        career.RankUp("college");
        CareerDefinition scientist = CareerCatalog.Get(CareerId.ComputerScientist);
        MeetGate(skills, scientist);
        long workbench = skills.Xp(SkillId.Workbench);

        Assert.Null(career.Enroll(scientist, skills));
        Assert.Equal(0, career.Xp);
        Assert.Equal(CareerRank.Apprentice, career.Rank);
        Assert.Equal(workbench, skills.Xp(SkillId.Workbench));
    }

    private static PlayerCareer Started(CareerDefinition definition, SkillBook? skills = null)
    {
        SkillBook book = skills ?? new SkillBook();
        PlayerCareer career = new PlayerCareer();
        career.TakeClass("college");
        MeetGate(book, definition);
        career.Enroll(definition, book);
        return career;
    }

    private static void MeetGate(SkillBook skills, CareerDefinition career)
    {
        foreach (KeyValuePair<SkillId, int> need in career.Gate)
        {
            skills.Load(need.Key, SkillCatalog.XpForLevel(need.Value));
        }
    }
}
