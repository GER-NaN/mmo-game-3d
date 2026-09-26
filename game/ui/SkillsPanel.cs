namespace MmoGame3d.Ui;

using System.Globalization;
using Godot;
using MmoGame3d.Rules.Skills;

// Your own skills and career. Skills are private (others see them only on your Whois
// page, if you show them); the career is public.
public partial class SkillsPanel : PanelContainer
{
    private static readonly Color Dim = new Color(1f, 1f, 1f, 0.55f);

    public void ShowProgress(int[] skills, long[] xp, int career, long careerXp, int rank, bool classTaken, int level)
    {
        GetNode<Label>("%Level").Text = "Level " + level;
        VBoxContainer list = GetNode<VBoxContainer>("%Skills");

        foreach (Node child in list.GetChildren())
        {
            list.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < skills.Length && i < xp.Length; i++)
        {
            if (!SkillCatalog.IsKnown(skills[i]))
            {
                continue;
            }

            SkillId skill = (SkillId)skills[i];
            int skillLevel = SkillCatalog.LevelFor(xp[i]);
            long from = SkillCatalog.XpForLevel(skillLevel);
            long to = SkillCatalog.XpForLevel(skillLevel + 1);

            HBoxContainer row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(new Label { Text = SkillCatalog.Name(skill), CustomMinimumSize = new Vector2(150, 0) });
            row.AddChild(new Label { Text = skillLevel.ToString(CultureInfo.InvariantCulture), CustomMinimumSize = new Vector2(30, 0) });
            row.AddChild(Bar(xp[i] - from, to - from));
            row.TooltipText = SkillCatalog.HowEarned(skill) + "  " + xp[i] + " xp";
            list.AddChild(row);
            list.AddChild(new Label { Text = SkillCatalog.HowEarned(skill), Modulate = Dim });
        }

        Label careerLine = GetNode<Label>("%Career");
        Control careerBar = GetNode<Control>("%CareerBar");

        foreach (Node child in careerBar.GetChildren())
        {
            careerBar.RemoveChild(child);
            child.QueueFree();
        }

        CareerDefinition? definition = career >= 0 ? CareerCatalog.Find(career) : null;

        if (definition == null)
        {
            careerLine.Text = classTaken
                ? "No career. The registrar at the college enrolls you in one."
                : "No career. A college teaches careers: there is one in town.";
            return;
        }

        CareerRank current = (CareerRank)rank;
        careerLine.Text = definition.Name + " · " + CareerCatalog.RankName(current);

        if (current < CareerRank.Elite)
        {
            long from = CareerCatalog.XpForRank(current);
            long to = CareerCatalog.XpForRank(current + 1);
            HBoxContainer row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            row.AddChild(Bar(careerXp - from, to - from));
            row.AddChild(new Label { Text = "to " + CareerCatalog.RankName(current + 1), Modulate = Dim });
            careerBar.AddChild(row);
        }
    }

    private static ProgressBar Bar(long value, long max)
    {
        return new ProgressBar
        {
            MinValue = 0,
            MaxValue = Mathf.Max(1, max),
            Value = Mathf.Clamp(value, 0, max),
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(120, 10),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
    }
}
