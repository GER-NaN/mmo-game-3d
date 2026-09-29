namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.College;
using MmoGame3d.Rules.Skills;

// The registrar's desk (the Class, then enrolling) or the professor's (rank-ups). It
// shows what the server last said about the player's progress; the server decides.
// Buttons never take focus, so walking goes on after a click.
public partial class CollegePanel : PanelContainer
{
    private static readonly Color Dim = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color Short = new Color(1f, 0.6f, 0.45f);
    private static readonly Color Met = new Color(0.6f, 1f, 0.7f);

    private static readonly string[] ClassText =
    {
        "A career is what you do with your skills. Skills grow by doing: walking, fixing things, cracking codes. A career takes a few of them further.",
        "Each career needs some skills at a level before you can start it. Once you are in, the skills it uses feed it, and so does using what it gives you.",
        "A career has five ranks: Apprentice, Graduate, Senior, Master, Elite. Your old professor, here at this college, signs off each one.",
        "You have one career at a time, and you do not need one at all. A change is permanent: you lose all progress in the career you leave. Your skills stay.",
    };

    private string _role = CollegePerson.Registrar;
    private SkillBook _skills = new SkillBook();
    private PlayerCareer _career = new PlayerCareer();

    public event Action? ClassPressed;
    public event Action<int>? EnrollPressed;
    public event Action? RankUpPressed;

    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

    public void Open(string role)
    {
        _role = role;
    }

    public void ShowProgress(int[] skills, long[] xp, int career, long careerXp, int rank, bool classTaken)
    {
        _skills = new SkillBook();

        for (int i = 0; i < skills.Length && i < xp.Length; i++)
        {
            if (SkillCatalog.IsKnown(skills[i]))
            {
                _skills.Load((SkillId)skills[i], xp[i]);
            }
        }

        _career = new PlayerCareer();
        CareerId? current = CareerCatalog.Find(career) != null ? (CareerId)career : null;
        _career.Load(current, careerXp, (CareerRank)rank, classTaken, "");
        Build();
    }

    private void Build()
    {
        VBoxContainer rows = GetNode<VBoxContainer>("%Rows");

        foreach (Node child in rows.GetChildren())
        {
            rows.RemoveChild(child);
            child.QueueFree();
        }

        if (_role == CollegePerson.Professor)
        {
            BuildProfessor(rows);
        }
        else if (!_career.ClassTaken)
        {
            BuildClass(rows);
        }
        else
        {
            BuildRegistrar(rows);
        }

        rows.AddChild(new Label { Text = "Esc or walk away to close", Modulate = Dim });
    }

    private void BuildClass(VBoxContainer rows)
    {
        Title(rows, "The Class");

        foreach (string paragraph in ClassText)
        {
            rows.AddChild(Wrapped(paragraph, Colors.White));
        }

        Button done = ActionButton("Finish the Class");
        done.Pressed += () => ClassPressed?.Invoke();
        rows.AddChild(done);
    }

    private void BuildRegistrar(VBoxContainer rows)
    {
        Title(rows, "Careers");
        rows.AddChild(Wrapped("Skills grow by doing things; the skills panel (" + MmoGame3d.Client.ClientSettings.KeyName("skills") + ") says how to earn each. A career needs some skills at a level before you can enroll.", Dim));
        string leaving = _career.Career.HasValue ? CareerCatalog.Get(_career.Career.Value).Name : "";

        foreach (CareerDefinition career in CareerCatalog.All)
        {
            rows.AddChild(new Label { Text = career.Name, ThemeTypeVariation = "HeaderSmall" });
            rows.AddChild(Wrapped(career.Summary, Dim));

            if (career.Gate.Count > 0)
            {
                rows.AddChild(new Label { Text = "  Skill levels needed (yours / needed):", Modulate = Dim });
            }

            foreach (KeyValuePair<SkillId, int> need in career.Gate)
            {
                int level = _skills.Level(need.Key);
                rows.AddChild(new Label { Text = "  " + SkillCatalog.Name(need.Key) + "  " + level + " / " + need.Value, Modulate = level >= need.Value ? Met : Short });

                if (level < need.Value)
                {
                    rows.AddChild(Wrapped("    " + SkillCatalog.HowEarned(need.Key), Dim));
                }
            }

            if (_career.Career == career.Id)
            {
                rows.AddChild(new Label { Text = "Your career: " + CareerCatalog.Title(career.Id, _career.Rank), Modulate = Met });
                continue;
            }

            bool open = PlayerCareer.Missing(career, _skills).Count == 0;
            string text = leaving.Length > 0 ? "Change to " + career.Name + " (you lose your " + leaving + " progress)" : "Enroll as " + career.Name;
            Button enroll = ActionButton(text);
            enroll.Disabled = !open;
            int id = (int)career.Id;
            enroll.Pressed += () => EnrollPressed?.Invoke(id);
            rows.AddChild(enroll);
        }
    }

    private void BuildProfessor(VBoxContainer rows)
    {
        Title(rows, "Your professor");

        if (!_career.Career.HasValue)
        {
            rows.AddChild(Wrapped("You have no career yet. The registrar enrolls you in one.", Colors.White));
            return;
        }

        rows.AddChild(new Label { Text = CareerCatalog.Title(_career.Career, _career.Rank) });

        if (_career.Rank == CareerRank.Elite)
        {
            rows.AddChild(Wrapped("Elite. There is nothing left to sign off.", Dim));
            return;
        }

        CareerRank next = _career.Rank + 1;
        long need = CareerCatalog.XpForRank(next);
        rows.AddChild(new Label { Text = "Career experience  " + _career.Xp + " / " + need + "  for " + CareerCatalog.RankName(next), Modulate = _career.CanRankUp() ? Met : Dim });
        Button rankUp = ActionButton("Ask for " + CareerCatalog.RankName(next));
        rankUp.Disabled = !_career.CanRankUp();
        rankUp.Pressed += () => RankUpPressed?.Invoke();
        rows.AddChild(rankUp);
    }

    private static void Title(VBoxContainer rows, string text)
    {
        Label title = new Label { Text = text };
        title.AddThemeFontSizeOverride("font_size", 20);
        rows.AddChild(title);
    }

    private static Label Wrapped(string text, Color color)
    {
        return new Label { Text = text, Modulate = color, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(440, 0) };
    }

    private static Button ActionButton(string text)
    {
        return new Button { Text = text, FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
    }
}
