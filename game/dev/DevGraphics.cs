namespace MmoGame3d.Dev;

using Godot;

/// <summary>
/// F9 in a debug build: the graphics panel (GraphicsPanel), shown or hidden. It is made on
/// first use, with the full-screen effects layer it drives. The key is fixed and not in
/// the key settings; a released game never has this node.
/// </summary>
public partial class DevGraphics : Node
{
    private static readonly PackedScene PanelScene = GD.Load<PackedScene>("res://game/dev/GraphicsPanel.tscn");
    private static readonly Shader EffectsShader = GD.Load<Shader>("res://game/dev/ScreenEffects.gdshader");

    private GraphicsPanel? _panel;

    public override void _Input(InputEvent input)
    {
        InputEventKey? key = input as InputEventKey;

        if (key == null || !key.Pressed || key.Echo || key.Keycode != Key.F9)
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        if (_panel == null)
        {
            Build();
            return;
        }

        _panel.Visible = !_panel.Visible;
    }

    private void Build()
    {
        // Over the world but under the game's screens (layer 1): the effects change the
        // world, not the HUD.
        CanvasLayer under = new CanvasLayer { Name = "ScreenEffects", Layer = 0 };
        AddChild(under);
        ShaderMaterial material = new ShaderMaterial { Shader = EffectsShader };

        // Set, not left to the shader: an unset value reads back empty, and the panel's
        // Reset needs the start values.
        material.SetShaderParameter("pixelate", false);
        material.SetShaderParameter("pixel_size", 4f);
        material.SetShaderParameter("posterize", false);
        material.SetShaderParameter("steps", 6f);
        material.SetShaderParameter("vignette", 0f);
        material.SetShaderParameter("grain", 0f);
        material.SetShaderParameter("fringe", 0f);
        material.SetShaderParameter("scanlines", 0f);
        material.SetShaderParameter("sharpen", 0f);
        material.SetShaderParameter("tint", Colors.White);
        material.SetShaderParameter("tint_amount", 0f);
        ColorRect effects = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false, Material = material };
        effects.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        under.AddChild(effects);

        CanvasLayer over = new CanvasLayer { Name = "Panel", Layer = 100 };
        AddChild(over);
        _panel = PanelScene.Instantiate<GraphicsPanel>();
        _panel.Effects = effects;
        over.AddChild(_panel);
    }
}
