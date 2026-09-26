namespace MmoGame3d.Ui;

using System;
using Godot;

/// <summary>
/// Who made the art and sound the game uses. The CC BY packs require their credit here
/// (assets/README.md lists each pack and its licence); the others are thanked.
/// </summary>
public partial class CreditsPanel : Control
{
    private static readonly string[][] Lines =
    {
        new[] { "Art", "" },
        new[] { "KayKit", "Kay Lousberg, www.kaylousberg.com (CC0)" },
        new[] { "Tiny Treats", "Isa Lousberg, www.isalousberg.com (CC0)" },
        new[] { "Sound", "" },
        new[] { "Surreal Drones, Weather Elements", "Helton Yan (CC BY 4.0)" },
        new[] { "Universal UI Sound Effects", "Nathan Gibson (CC BY 4.0)" },
        new[] { "Troubadeck music loops", "Abstraction (CC0)" },
        new[] { "Interface Bleeps", "Bleeoop" },
        new[] { "8-bit and 16-bit sound effects", "jdwasabi" },
        new[] { "Sound Essentials", "Nox Sound (CC0)" },
    };

    public event Action? Closed;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        ColorRect dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.6f) };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(dim);
        CenterContainer center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);
        PanelContainer panel = new PanelContainer();
        center.AddChild(panel);
        MarginContainer margin = new MarginContainer();

        foreach (string side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
        {
            margin.AddThemeConstantOverride(side, 20);
        }

        panel.AddChild(margin);
        VBoxContainer rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 6);
        margin.AddChild(rows);
        Label title = new Label { Text = "Credits" };
        title.AddThemeFontSizeOverride("font_size", 26);
        rows.AddChild(title);

        foreach (string[] line in Lines)
        {
            if (line[1].Length == 0)
            {
                Label heading = new Label { Text = line[0], Modulate = new Color(1f, 0.85f, 0.5f) };
                heading.AddThemeFontSizeOverride("font_size", 18);
                rows.AddChild(heading);
                continue;
            }

            HBoxContainer row = new HBoxContainer();
            row.AddChild(new Label { Text = line[0], CustomMinimumSize = new Vector2(300, 0) });
            row.AddChild(new Label { Text = line[1], Modulate = new Color(1f, 1f, 1f, 0.75f) });
            rows.AddChild(row);
        }

        Button back = new Button { Text = "Back" };
        back.Pressed += () => Closed?.Invoke();
        rows.AddChild(back);
    }
}
