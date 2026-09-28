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
    public event Action? Closed;

    // The card sits top right, under the HUD's name line, and grows down, so a long
    // history stays on screen (PlantCard.tscn).
    public override void _Ready()
    {
        GetNode<Button>("%Close").Pressed += () => Closed?.Invoke();
    }

    public void ShowPlant(long plantId, string name, string creator, string madeOn, string design, string[] history)
    {
        GetNode<Label>("%Title").Text = name.Length > 0 ? name : "House plant #" + plantId;
        GetNode<Label>("%Number").Text = "House plant #" + plantId + ", one of a kind";
        GetNode<Label>("%Creator").Text = "Created by: " + creator;
        GetNode<Label>("%MadeOn").Text = "Made on: " + madeOn;

        PlantDesign? parsed = PlantDesign.Parse(design);
        VBoxContainer parts = GetNode<VBoxContainer>("%Parts");
        GetNode<Label>("%MadeOfTitle").Visible = parsed != null;
        parts.Visible = parsed != null;

        if (parsed != null)
        {
            parts.AddChild(Dim(PlantParts.Describe(parsed.Pot)));

            foreach (PlantPiece piece in parsed.Pieces)
            {
                parts.AddChild(Dim(PlantParts.Describe(piece.Id)));
            }
        }

        VBoxContainer lines = GetNode<VBoxContainer>("%History");

        foreach (string line in history)
        {
            lines.AddChild(Dim(line));
        }
    }

    private static Label Dim(string text)
    {
        return new Label { Text = text, Modulate = new Color(1f, 1f, 1f, 0.75f), AutowrapMode = TextServer.AutowrapMode.WordSmart };
    }
}
