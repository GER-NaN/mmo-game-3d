namespace MmoGame3d.Ui;

using Godot;

/// <summary>
/// A screen's close button, in the top right-hand corner of the panel it is on, clear of
/// the screen's content (playtest 2026-09-29): placed by the panel's corner, not by the
/// row it sits in, so every screen that uses CloseButton.tscn gets it there.
/// </summary>
public partial class CornerClose : Button
{
    // From the panel's edges; a placeholder until seen.
    private const float Inset = 6f;

    private Control? _panel;

    public override void _Ready()
    {
        TopLevel = true;

        // The nearest panel up the tree: the screen's own, or the one centred inside it.
        for (Node? node = GetParent(); node != null; node = node.GetParent())
        {
            if (node is PanelContainer || node is Panel)
            {
                _panel = (Control)node;
                break;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_panel == null)
        {
            return;
        }

        Rect2 box = _panel.GetGlobalRect();
        GlobalPosition = new Vector2(box.End.X - Size.X - Inset, box.Position.Y + Inset);
    }
}
