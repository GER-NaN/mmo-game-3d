namespace MmoGame3d.Ui;

using System;
using System.Globalization;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Achievements;
using MmoGame3d.Rules.Skills;

// Your own skills and career. Skills are private (others see them only on your Whois
// page, if you show them); the career is public.
public partial class SkillsPanel : PanelContainer
{
    private static readonly Color Dim = new Color(1f, 1f, 1f, 0.55f);

    // Click a skill's name to read what it is and how to earn it, below the list; one at
    // a time, so the panel stays short.
    private SkillId _selected = SkillId.Agility;
    private Label? _detail;

    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

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
            CardButton name = new CardButton
            {
                Text = SkillCatalog.Name(skill),
                Flat = true,
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
                CustomMinimumSize = new Vector2(150, 0),
                CardTitle = SkillCatalog.Name(skill),
                CardDetail = "Level " + skillLevel + ", " + xp[i] + " of " + to + " xp",
                CardDescription = SkillCatalog.About(skill) + "\nEarn it: " + SkillCatalog.HowEarned(skill),
            };
            name.Pressed += () => Select(skill);
            row.AddChild(name);
            row.AddChild(new Label { Text = skillLevel.ToString(CultureInfo.InvariantCulture), CustomMinimumSize = new Vector2(30, 0) });
            row.AddChild(Bar(xp[i] - from, to - from));
            row.TooltipText = xp[i] + " xp";
            list.AddChild(row);
        }

        _detail = Wrapped("", Dim);
        list.AddChild(_detail);
        Select(_selected);

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

    // Below the career: every achievement, earned ones bright, the rest dim; hover for how.
    public void ShowAchievements(string[] earned)
    {
        VBoxContainer rows = GetNode<VBoxContainer>("%Rows");
        GridContainer? grid = rows.GetNodeOrNull<GridContainer>("Achievements");

        if (grid == null)
        {
            Label title = new Label { Name = "AchievementsTitle" };
            title.AddThemeFontSizeOverride("font_size", 18);
            rows.AddChild(title);
            grid = new GridContainer { Name = "Achievements", Columns = 2 };
            grid.AddThemeConstantOverride("h_separation", 16);
            rows.AddChild(grid);
        }

        foreach (Node child in grid.GetChildren())
        {
            grid.RemoveChild(child);
            child.QueueFree();
        }

        HashSet<string> done = new HashSet<string>(earned);
        int count = 0;

        foreach (Achievement achievement in Achievements.All)
        {
            bool has = done.Contains(achievement.Id);
            count += has ? 1 : 0;
            Label label = new Label
            {
                Text = (has ? "* " : "- ") + achievement.Title,
                TooltipText = achievement.Text,
                MouseFilter = MouseFilterEnum.Pass,
                Modulate = has ? new Color(1f, 0.9f, 0.45f) : Dim,
            };
            grid.AddChild(label);
        }

        rows.GetNode<Label>("AchievementsTitle").Text = "Achievements  " + count + " of " + Achievements.All.Count;
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

    private void Select(SkillId skill)
    {
        _selected = skill;

        if (_detail != null)
        {
            _detail.Text = SkillCatalog.Name(skill) + ": " + SkillCatalog.About(skill) + "\nEarn it: " + SkillCatalog.HowEarned(skill);
        }
    }

    private static Label Wrapped(string text, Color color)
    {
        return new Label { Text = text, Modulate = color, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(360, 0) };
    }
}
