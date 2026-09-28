namespace MmoGame3d.Gardening;

using System;
using Godot;
using MmoGame3d.Rules.Gardening;

/// <summary>
/// A house plant's provenance, shown when it is inspected: its number and name, who made
/// it and when, what it is made of, and everything that has happened to it since.
/// </summary>
public partial class PlantCard : PanelContainer
{
    private static readonly PackedScene CloseScene = GD.Load<PackedScene>("res://game/ui/CloseButton.tscn");

    public event Action? Closed;

    public void ShowPlant(long plantId, string name, string creator, string madeOn, string design, string[] history)
    {
        CustomMinimumSize = new Vector2(380, 0);
        // Top right, under the HUD's name line, growing down: a long history stays on
        // screen.
        SetAnchorsPreset(LayoutPreset.TopRight);
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.End;
        OffsetLeft = -400;
        OffsetRight = -20;
        OffsetTop = 60;

        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 14);
        }

        AddChild(margin);
        VBoxContainer rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 6);
        margin.AddChild(rows);

        HBoxContainer titleRow = new HBoxContainer();
        Label title = new Label { Text = name.Length > 0 ? name : "House plant #" + plantId, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 22);
        titleRow.AddChild(title);
        Button close = CloseScene.Instantiate<Button>();
        close.Pressed += () => Closed?.Invoke();
        titleRow.AddChild(close);
        rows.AddChild(titleRow);
        rows.AddChild(new Label { Text = "House plant #" + plantId + ", one of a kind" });
        rows.AddChild(new Label { Text = "Created by: " + creator });
        rows.AddChild(new Label { Text = "Made on: " + madeOn });

        PlantDesign? parsed = PlantDesign.Parse(design);

        if (parsed != null)
        {
            rows.AddChild(Heading("Made of"));
            rows.AddChild(Dim(PlantParts.Describe(parsed.Pot)));

            foreach (PlantPiece piece in parsed.Pieces)
            {
                rows.AddChild(Dim(PlantParts.Describe(piece.Id)));
            }
        }

        rows.AddChild(Heading("History"));

        foreach (string line in history)
        {
            rows.AddChild(Dim(line));
        }
    }

    private static Label Heading(string text)
    {
        Label label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 16);
        return label;
    }

    private static Label Dim(string text)
    {
        return new Label { Text = text, Modulate = new Color(1f, 1f, 1f, 0.75f), AutowrapMode = TextServer.AutowrapMode.WordSmart };
    }
}
