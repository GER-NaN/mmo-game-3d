namespace MmoGame3d.Ui;

using System;
using Godot;

/// <summary>
/// Who made the art and sound the game uses, written in CreditsPanel.tscn. The CC BY
/// packs require their credit there (assets/README.md lists each pack and its licence);
/// the others are thanked.
/// </summary>
public partial class CreditsPanel : Control
{
    public event Action? Closed;

    public override void _Ready()
    {
        GetNode<Button>("%Back").Pressed += () => Closed?.Invoke();
    }
}
