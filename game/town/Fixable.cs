namespace MmoGame3d.Town;

using Godot;
using MmoGame3d.Interact;

/// <summary>
/// A small thing in town that breaks now and then (a hydrant, a bench): the Field repair
/// skill's act. The prop is a child; this adds the broken state, synced so everyone sees
/// the sparks, and the prompt. The server breaks them (ServerFixables).
/// </summary>
public partial class Fixable : Interactable
{
    [Export]
    public string FixableName { get; set; } = "fire hydrant";

    [Export]
    public bool Broken { get; set; }

    public override string Prompt
    {
        get { return Broken ? "Fix the " + FixableName : ""; }
    }

    private double _flicker;

    // Sparks and a flickering glow, so a broken one is seen from down the street.
    public override void _Process(double delta)
    {
        if (Multiplayer.IsServer())
        {
            return;
        }

        GetNode<CpuParticles3D>("Sparks").Emitting = Broken;
        OmniLight3D glow = GetNode<OmniLight3D>("Glow");
        glow.Visible = Broken;

        if (Broken)
        {
            _flicker += delta;
            glow.LightEnergy = 1.5f + (1.5f * Mathf.Abs(Mathf.Sin((float)_flicker * 17f) * Mathf.Sin((float)_flicker * 5f)));
        }
    }
}
