namespace MmoGame3d.Dev;

using System;
using Godot;

// One knob on the graphics panel: what it is called, how to read and set what it turns,
// and the value it had when the game started, for Reset.
public class DevKnob
{
    public string Tab { get; set; } = "";
    public string Label { get; set; } = "";

    // Its name in a saved file.
    public string Key { get; set; } = "";

    public DevKnobKind Kind { get; set; }
    public float Min { get; set; }
    public float Max { get; set; } = 1f;
    public float Step { get; set; } = 0.01f;
    public string[] Choices { get; set; } = new string[0];

    // On the world (its environment, sun or camera): read again when the world is new.
    public bool OnWorld { get; set; }

    public Func<Variant> Get { get; set; } = () => default;
    public Action<Variant> Set { get; set; } = value => { };
    public Variant Default { get; set; }

    public Control? Control { get; set; }

    // A slider's value, beside it.
    public Label? Shown { get; set; }
}
