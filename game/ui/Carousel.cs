namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;

// A row of things to pick from, the chosen one in the middle and framed, arrows either
// side. The wheel steps it, and resting the mouse near either end steps it that way.
// The caller gives the things (buttons, usually) and hears which one is chosen.
public partial class Carousel : HBoxContainer
{
    // Seconds between steps while the mouse rests near an end.
    private const double EdgeRepeat = 0.4;

    // How near an end counts, as a share of the strip's width.
    private const float EdgeShare = 0.12f;

    // How fast the row slides to the chosen one, per second.
    private const float Slide = 12f;

    // Room round each thing, in pixels.
    private const float Spacing = 18f;

    // How many things show at once; odd, so one is in the middle.
    [Export]
    public int ShowCount { get; set; } = 5;

    // (index) when the chosen thing changes.
    public event Action<int>? ChosenChanged;

    // (index) when the thing already in the middle is pressed.
    public event Action<int>? ChosenPressed;

    private readonly List<Control> _items = new List<Control>();
    private Control _strip = null!;
    private float _shown;
    private double _sinceEdgeStep;

    public int Chosen { get; private set; }

    public Control? ChosenItem
    {
        get { return Chosen < _items.Count ? _items[Chosen] : null; }
    }

    public override void _Ready()
    {
        _strip = GetNode<Control>("%Strip");
        GetNode<Button>("%Prev").Pressed += () => Choose(Chosen - 1);
        GetNode<Button>("%Next").Pressed += () => Choose(Chosen + 1);
    }

    // The things, in order, and which one starts chosen. The carousel owns them after.
    public void SetItems(List<Control> items, int chosen)
    {
        foreach (Control old in _items)
        {
            old.QueueFree();
        }

        _items.Clear();
        float tallest = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            Control item = items[i];
            int index = i;
            _items.Add(item);
            _strip.AddChild(item);
            tallest = Math.Max(tallest, item.CustomMinimumSize.Y);

            // A press on one at the side brings it to the middle.
            item.GuiInput += input =>
            {
                InputEventMouseButton? click = input as InputEventMouseButton;

                if (click == null || !click.Pressed || click.ButtonIndex != MouseButton.Left)
                {
                    return;
                }

                if (index == Chosen)
                {
                    ChosenPressed?.Invoke(index);
                }
                else
                {
                    Choose(index);
                }
            };
        }

        _strip.CustomMinimumSize = new Vector2((SlotWidth() * ShowCount) + Spacing, tallest + (Spacing * 2f));
        Chosen = Math.Clamp(chosen, 0, Math.Max(0, items.Count - 1));
        _shown = Chosen;
        Place();
    }

    public void Choose(int index)
    {
        if (_items.Count == 0)
        {
            return;
        }

        int clamped = Math.Clamp(index, 0, _items.Count - 1);

        if (clamped == Chosen)
        {
            return;
        }

        Chosen = clamped;
        ChosenChanged?.Invoke(Chosen);
    }

    public override void _Process(double delta)
    {
        StepAtEdges(delta);
        _shown = Mathf.MoveToward(_shown, Chosen, (float)delta * Slide * Math.Max(1f, Math.Abs(Chosen - _shown)));
        Place();
    }

    // Here rather than on the strip: the things over it would take the wheel first.
    public override void _Input(InputEvent input)
    {
        InputEventMouseButton? wheel = input as InputEventMouseButton;

        if (wheel != null && wheel.Pressed && IsVisibleInTree() && _strip.GetGlobalRect().HasPoint(wheel.Position)
            && (wheel.ButtonIndex == MouseButton.WheelUp || wheel.ButtonIndex == MouseButton.WheelDown))
        {
            Choose(Chosen + (wheel.ButtonIndex == MouseButton.WheelUp ? -1 : 1));
            GetViewport().SetInputAsHandled();
        }
    }

    private void StepAtEdges(double delta)
    {
        Rect2 rect = _strip.GetGlobalRect();
        Vector2 mouse = GetGlobalMousePosition();

        if (!IsVisibleInTree() || !rect.HasPoint(mouse))
        {
            _sinceEdgeStep = 0;
            return;
        }

        float edge = rect.Size.X * EdgeShare;
        int way = 0;

        if (mouse.X < rect.Position.X + edge)
        {
            way = -1;
        }
        else if (mouse.X > rect.End.X - edge)
        {
            way = 1;
        }

        if (way == 0)
        {
            _sinceEdgeStep = 0;
            return;
        }

        _sinceEdgeStep += delta;

        if (_sinceEdgeStep >= EdgeRepeat)
        {
            _sinceEdgeStep = 0;
            Choose(Chosen + way);
        }
    }

    private float SlotWidth()
    {
        float widest = 0f;

        foreach (Control item in _items)
        {
            widest = Math.Max(widest, item.CustomMinimumSize.X);
        }

        return widest + Spacing;
    }

    // Each thing at its slot, the chosen one in the middle; those past the ends hidden,
    // so nothing can be pressed outside the strip.
    private void Place()
    {
        if (_strip == null)
        {
            return;
        }

        float slot = SlotWidth();
        Vector2 middle = _strip.Size / 2f;
        int half = ShowCount / 2;

        for (int i = 0; i < _items.Count; i++)
        {
            Control item = _items[i];
            float along = i - _shown;
            item.Size = item.CustomMinimumSize;
            item.Position = new Vector2(middle.X + (along * slot) - (item.Size.X / 2f), middle.Y - (item.Size.Y / 2f));
            item.Visible = Math.Abs(along) <= half + 0.5f;
        }

        Panel chosen = GetNode<Panel>("%Chosen");
        chosen.Size = new Vector2(slot, _strip.Size.Y - 2f);
        chosen.Position = new Vector2(middle.X - (slot / 2f), 1f);
        GetNode<Button>("%Prev").Disabled = Chosen <= 0;
        GetNode<Button>("%Next").Disabled = Chosen >= _items.Count - 1;
    }
}
