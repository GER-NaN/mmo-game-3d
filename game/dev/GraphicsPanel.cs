namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Zones;

/// <summary>
/// A dev panel of knobs on how the world looks (F9, debug builds only): edges, pixels,
/// tone, light, air, the camera and the hour, and screen effects. A knob applies at once;
/// Reset puts back what the game started with; Save writes every value as JSON, with a
/// screenshot taken without the panel, to graphics-saves/. Nothing is kept between
/// launches. The sun's strength and the sky's brightness are not here: DayNight sets them
/// every frame, so the hour stands in for them.
/// </summary>
public partial class GraphicsPanel : PanelContainer
{
    private const string SaveFolder = "res://graphics-saves";

    // How often it looks for a new world (after leaving and coming back).
    private const double LookEvery = 0.5;

    private static readonly string[] Msaa = { "Off", "2x", "4x", "8x" };
    private static readonly string[] ScreenAA = { "Off", "FXAA", "SMAA" };
    private static readonly string[] Anisotropic = { "Off", "2x", "4x", "8x", "16x" };
    private static readonly string[] Scaling = { "Bilinear", "Nearest", "FSR 1", "FSR 2" };
    private static readonly Viewport.Scaling3DModeEnum[] ScalingModes =
    {
        Viewport.Scaling3DModeEnum.Bilinear, Viewport.Scaling3DModeEnum.Nearest, Viewport.Scaling3DModeEnum.Fsr, Viewport.Scaling3DModeEnum.Fsr2,
    };

    private static readonly string[] Tonemappers = { "Linear", "Reinhard", "Filmic", "ACES", "AgX" };
    private static readonly string[] GlowBlends = { "Additive", "Screen", "Softlight", "Replace", "Mix" };

    private readonly List<DevKnob> _knobs = new List<DevKnob>();
    private readonly CameraAttributesPractical _cameraLook = new CameraAttributesPractical();

    private Node3D? _world;
    private CameraAttributes? _cameraWas;
    private double _sinceLook = LookEvery;
    private bool _holdHour;
    private float _hour = 12f;

    // Frames left before the screenshot, with the panel hidden; 0 for none.
    private int _shotIn;
    private string _shotPath = "";

    // The full-screen effects, drawn over the world (DevGraphics makes it).
    public ColorRect? Effects { get; set; }

    public override void _Ready()
    {
        // Small print: the panel is meant to leave the world in view.
        Theme = new Theme { DefaultFontSize = 12 };
        GetNode<Button>("%Reset").Pressed += Reset;
        GetNode<Button>("%Save").Pressed += Save;
        GetNode<Button>("%Hide").Pressed += () => Visible = false;

        AddKnobs();

        foreach (DevKnob knob in _knobs)
        {
            AddRow(knob);
        }

        FindWorld();
        CaptureDefaults(false);
        ShowValues();
    }

    public override void _Process(double delta)
    {
        _sinceLook += delta;

        if (_sinceLook >= LookEvery)
        {
            _sinceLook = 0;
            FindWorld();
        }

        if (_holdHour)
        {
            DayNight()?.SetTime(_hour * 3600.0);
        }

        if (_shotIn > 0)
        {
            _shotIn--;

            if (_shotIn == 0)
            {
                GetViewport().GetTexture().GetImage().SavePng(_shotPath);
                Visible = true;
            }
        }
    }

    private void AddKnobs()
    {
        // Edges.
        Choice("Edges", "MSAA", "msaa", Msaa, () => (int)GetViewport().Msaa3D, value => GetViewport().Msaa3D = (Viewport.Msaa)value);
        Choice("Edges", "Screen AA", "screen_aa", ScreenAA, () => (int)GetViewport().ScreenSpaceAA, value => GetViewport().ScreenSpaceAA = (Viewport.ScreenSpaceAAEnum)value);
        Toggle("Edges", "TAA (temporal)", "taa", () => GetViewport().UseTaa, value => GetViewport().UseTaa = value);
        Toggle("Edges", "Debanding", "debanding", () => GetViewport().UseDebanding, value => GetViewport().UseDebanding = value);
        Choice("Edges", "Texture filter", "anisotropic", Anisotropic, () => (int)GetViewport().AnisotropicFilteringLevel, value => GetViewport().AnisotropicFilteringLevel = (Viewport.AnisotropicFiltering)value);

        // Pixels.
        Slider("Pixels", "3D resolution", "scale_3d", 0.1f, 1f, 0.05f, () => GetViewport().Scaling3DScale, value => GetViewport().Scaling3DScale = value);
        Choice("Pixels", "Upscale", "scaling_mode", Scaling, () => Math.Max(0, Array.IndexOf(ScalingModes, GetViewport().Scaling3DMode)), value => GetViewport().Scaling3DMode = ScalingModes[value]);
        Toggle("Pixels", "Pixelate", "pixelate", () => Effect("pixelate").AsBool(), value => SetEffect("pixelate", value));
        Slider("Pixels", "Pixel size", "pixel_size", 1f, 24f, 1f, () => Effect("pixel_size").AsSingle(), value => SetEffect("pixel_size", value));
        Toggle("Pixels", "Posterize", "posterize", () => Effect("posterize").AsBool(), value => SetEffect("posterize", value));
        Slider("Pixels", "Colour steps", "steps", 2f, 24f, 1f, () => Effect("steps").AsSingle(), value => SetEffect("steps", value));

        // Tone and colour.
        Choice("Tone", "Tonemapper", "tonemapper", Tonemappers, () => (int)(Env()?.TonemapMode ?? Godot.Environment.ToneMapper.Filmic), value => WithEnv(env => env.TonemapMode = (Godot.Environment.ToneMapper)value), true);
        Slider("Tone", "Exposure", "exposure", 0.1f, 4f, 0.05f, () => Env()?.TonemapExposure ?? 1f, value => WithEnv(env => env.TonemapExposure = value), true);
        Slider("Tone", "White", "white", 0.5f, 16f, 0.1f, () => Env()?.TonemapWhite ?? 1f, value => WithEnv(env => env.TonemapWhite = value), true);
        Toggle("Tone", "Colour adjust", "adjust", () => Env()?.AdjustmentEnabled ?? false, value => WithEnv(env => env.AdjustmentEnabled = value), true);
        Slider("Tone", "Brightness", "brightness", 0f, 2f, 0.02f, () => Env()?.AdjustmentBrightness ?? 1f, value => WithEnv(env => env.AdjustmentBrightness = value), true);
        Slider("Tone", "Contrast", "contrast", 0f, 2f, 0.02f, () => Env()?.AdjustmentContrast ?? 1f, value => WithEnv(env => env.AdjustmentContrast = value), true);
        Slider("Tone", "Saturation", "saturation", 0f, 2f, 0.02f, () => Env()?.AdjustmentSaturation ?? 1f, value => WithEnv(env => env.AdjustmentSaturation = value), true);

        // Light.
        Toggle("Light", "Glow", "glow", () => Env()?.GlowEnabled ?? false, value => WithEnv(env => env.GlowEnabled = value), true);
        Slider("Light", "Glow intensity", "glow_intensity", 0f, 4f, 0.05f, () => Env()?.GlowIntensity ?? 0.8f, value => WithEnv(env => env.GlowIntensity = value), true);
        Slider("Light", "Glow strength", "glow_strength", 0f, 2f, 0.05f, () => Env()?.GlowStrength ?? 1f, value => WithEnv(env => env.GlowStrength = value), true);
        Slider("Light", "Glow bloom", "glow_bloom", 0f, 1f, 0.02f, () => Env()?.GlowBloom ?? 0f, value => WithEnv(env => env.GlowBloom = value), true);
        Slider("Light", "Glow threshold", "glow_threshold", 0f, 4f, 0.05f, () => Env()?.GlowHdrThreshold ?? 1f, value => WithEnv(env => env.GlowHdrThreshold = value), true);
        Choice("Light", "Glow blend", "glow_blend", GlowBlends, () => (int)(Env()?.GlowBlendMode ?? Godot.Environment.GlowBlendModeEnum.Softlight), value => WithEnv(env => env.GlowBlendMode = (Godot.Environment.GlowBlendModeEnum)value), true);
        Toggle("Light", "SSAO (corner shade)", "ssao", () => Env()?.SsaoEnabled ?? false, value => WithEnv(env => env.SsaoEnabled = value), true);
        Slider("Light", "SSAO intensity", "ssao_intensity", 0f, 8f, 0.1f, () => Env()?.SsaoIntensity ?? 2f, value => WithEnv(env => env.SsaoIntensity = value), true);
        Slider("Light", "SSAO radius", "ssao_radius", 0.1f, 4f, 0.05f, () => Env()?.SsaoRadius ?? 1f, value => WithEnv(env => env.SsaoRadius = value), true);
        Toggle("Light", "SSIL (bounce)", "ssil", () => Env()?.SsilEnabled ?? false, value => WithEnv(env => env.SsilEnabled = value), true);
        Slider("Light", "SSIL intensity", "ssil_intensity", 0f, 4f, 0.05f, () => Env()?.SsilIntensity ?? 1f, value => WithEnv(env => env.SsilIntensity = value), true);
        Toggle("Light", "SSR (reflections)", "ssr", () => Env()?.SsrEnabled ?? false, value => WithEnv(env => env.SsrEnabled = value), true);
        Toggle("Light", "SDFGI (global light)", "sdfgi", () => Env()?.SdfgiEnabled ?? false, value => WithEnv(env => env.SdfgiEnabled = value), true);
        Slider("Light", "Sky light share", "sky_contribution", 0f, 1f, 0.02f, () => Env()?.AmbientLightSkyContribution ?? 1f, value => WithEnv(env => env.AmbientLightSkyContribution = value), true);
        Toggle("Light", "Sun shadows", "shadows", () => Sun()?.ShadowEnabled ?? true, value => WithSun(sun => sun.ShadowEnabled = value), true);
        Slider("Light", "Shadow blur", "shadow_blur", 0f, 4f, 0.05f, () => Sun()?.ShadowBlur ?? 1f, value => WithSun(sun => sun.ShadowBlur = value), true);
        Slider("Light", "Sun size", "sun_size", 0f, 10f, 0.1f, () => Sun()?.LightAngularDistance ?? 0f, value => WithSun(sun => sun.LightAngularDistance = value), true);

        // Air.
        Toggle("Air", "Fog", "fog", () => Env()?.FogEnabled ?? false, value => WithEnv(env => env.FogEnabled = value), true);
        Slider("Air", "Fog density", "fog_density", 0f, 0.05f, 0.0005f, () => Env()?.FogDensity ?? 0.01f, value => WithEnv(env => env.FogDensity = value), true);
        ColorKnob("Air", "Fog colour", "fog_colour", () => Env()?.FogLightColor ?? Colors.Gray, value => WithEnv(env => env.FogLightColor = value), true);
        Slider("Air", "Fog on the sky", "fog_sky", 0f, 1f, 0.02f, () => Env()?.FogSkyAffect ?? 1f, value => WithEnv(env => env.FogSkyAffect = value), true);
        Slider("Air", "Sun through fog", "fog_sun", 0f, 1f, 0.02f, () => Env()?.FogSunScatter ?? 0f, value => WithEnv(env => env.FogSunScatter = value), true);
        Slider("Air", "Distance haze", "fog_aerial", 0f, 1f, 0.02f, () => Env()?.FogAerialPerspective ?? 0f, value => WithEnv(env => env.FogAerialPerspective = value), true);
        Toggle("Air", "Volumetric fog", "volumetric", () => Env()?.VolumetricFogEnabled ?? false, value => WithEnv(env => env.VolumetricFogEnabled = value), true);
        Slider("Air", "Volumetric density", "volumetric_density", 0f, 0.1f, 0.001f, () => Env()?.VolumetricFogDensity ?? 0.05f, value => WithEnv(env => env.VolumetricFogDensity = value), true);
        Slider("Air", "Volumetric reach", "volumetric_length", 16f, 512f, 8f, () => Env()?.VolumetricFogLength ?? 64f, value => WithEnv(env => env.VolumetricFogLength = value), true);

        // The camera and the hour.
        Slider("Camera", "Field of view", "fov", 30f, 110f, 1f, () => Camera()?.Fov ?? 75f, value => WithCamera(camera => camera.Fov = value), true);
        Toggle("Camera", "Blur far", "dof_far", () => _cameraLook.DofBlurFarEnabled, value => WithLook(look => look.DofBlurFarEnabled = value));
        Slider("Camera", "Far blur from", "dof_far_distance", 2f, 200f, 1f, () => _cameraLook.DofBlurFarDistance, value => WithLook(look => look.DofBlurFarDistance = value));
        Slider("Camera", "Far blur fade", "dof_far_transition", 0f, 50f, 0.5f, () => _cameraLook.DofBlurFarTransition, value => WithLook(look => look.DofBlurFarTransition = value));
        Toggle("Camera", "Blur near", "dof_near", () => _cameraLook.DofBlurNearEnabled, value => WithLook(look => look.DofBlurNearEnabled = value));
        Slider("Camera", "Near blur to", "dof_near_distance", 0f, 10f, 0.1f, () => _cameraLook.DofBlurNearDistance, value => WithLook(look => look.DofBlurNearDistance = value));
        Slider("Camera", "Blur amount", "dof_amount", 0f, 1f, 0.01f, () => _cameraLook.DofBlurAmount, value => WithLook(look => look.DofBlurAmount = value));
        Toggle("Camera", "Hold the hour", "hold_hour", () => _holdHour, value => _holdHour = value);
        Slider("Camera", "Hour", "hour", 0f, 24f, 0.25f, () => _holdHour ? _hour : ClockHour(), value =>
        {
            _hour = value;
            DayNight()?.SetTime(value * 3600.0);
        });

        // Screen effects.
        Slider("Screen", "Vignette", "vignette", 0f, 1f, 0.02f, () => Effect("vignette").AsSingle(), value => SetEffect("vignette", value));
        Slider("Screen", "Film grain", "grain", 0f, 0.4f, 0.01f, () => Effect("grain").AsSingle(), value => SetEffect("grain", value));
        Slider("Screen", "Colour fringe", "fringe", 0f, 8f, 0.1f, () => Effect("fringe").AsSingle(), value => SetEffect("fringe", value));
        Slider("Screen", "Scanlines", "scanlines", 0f, 1f, 0.02f, () => Effect("scanlines").AsSingle(), value => SetEffect("scanlines", value));
        Slider("Screen", "Sharpen", "sharpen", 0f, 2f, 0.05f, () => Effect("sharpen").AsSingle(), value => SetEffect("sharpen", value));
        ColorKnob("Screen", "Tint", "tint", () => Effect("tint").AsColor(), value => SetEffect("tint", value));
        Slider("Screen", "Tint amount", "tint_amount", 0f, 1f, 0.02f, () => Effect("tint_amount").AsSingle(), value => SetEffect("tint_amount", value));
    }

    private void Toggle(string tab, string label, string key, Func<bool> get, Action<bool> set, bool onWorld = false)
    {
        _knobs.Add(new DevKnob { Tab = tab, Label = label, Key = key, Kind = DevKnobKind.Toggle, OnWorld = onWorld, Get = () => get(), Set = value => set(value.AsBool()) });
    }

    private void Slider(string tab, string label, string key, float min, float max, float step, Func<float> get, Action<float> set, bool onWorld = false)
    {
        _knobs.Add(new DevKnob { Tab = tab, Label = label, Key = key, Kind = DevKnobKind.Slider, Min = min, Max = max, Step = step, OnWorld = onWorld, Get = () => get(), Set = value => set(value.AsSingle()) });
    }

    private void Choice(string tab, string label, string key, string[] choices, Func<int> get, Action<int> set, bool onWorld = false)
    {
        _knobs.Add(new DevKnob { Tab = tab, Label = label, Key = key, Kind = DevKnobKind.Choice, Choices = choices, OnWorld = onWorld, Get = () => get(), Set = value => set(value.AsInt32()) });
    }

    private void ColorKnob(string tab, string label, string key, Func<Color> get, Action<Color> set, bool onWorld = false)
    {
        _knobs.Add(new DevKnob { Tab = tab, Label = label, Key = key, Kind = DevKnobKind.Color, OnWorld = onWorld, Get = () => get(), Set = value => set(value.AsColor()) });
    }

    // A row in the knob's tab: its name, and the control that turns it.
    private void AddRow(DevKnob knob)
    {
        GridContainer rows = TabRows(knob.Tab);
        HBoxContainer row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 6);
        rows.AddChild(row);

        switch (knob.Kind)
        {
            case DevKnobKind.Toggle:
                CheckBox box = new CheckBox { Text = knob.Label, FocusMode = FocusModeEnum.None };
                box.Toggled += on =>
                {
                    knob.Set(on);
                    UpdateEffects();
                };
                row.AddChild(box);
                knob.Control = box;
                return;
            case DevKnobKind.Slider:
                row.AddChild(new Label { Text = knob.Label, CustomMinimumSize = new Vector2(120, 0) });
                HSlider slider = new HSlider { MinValue = knob.Min, MaxValue = knob.Max, Step = knob.Step, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, FocusMode = FocusModeEnum.None };
                Label shown = new Label { CustomMinimumSize = new Vector2(52, 0), HorizontalAlignment = HorizontalAlignment.Right };
                slider.ValueChanged += value =>
                {
                    knob.Set((float)value);
                    shown.Text = Number((float)value);
                    UpdateEffects();
                };
                row.AddChild(slider);
                row.AddChild(shown);
                knob.Control = slider;
                knob.Shown = shown;
                return;
            case DevKnobKind.Choice:
                row.AddChild(new Label { Text = knob.Label, CustomMinimumSize = new Vector2(120, 0) });
                OptionButton options = new OptionButton { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };

                foreach (string choice in knob.Choices)
                {
                    options.AddItem(choice);
                }

                options.ItemSelected += index => knob.Set((int)index);
                row.AddChild(options);
                knob.Control = options;
                return;
            case DevKnobKind.Color:
                row.AddChild(new Label { Text = knob.Label, CustomMinimumSize = new Vector2(120, 0) });
                ColorPickerButton picker = new ColorPickerButton { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(80, 20), SizeFlagsHorizontal = SizeFlags.ExpandFill };
                picker.ColorChanged += colour =>
                {
                    knob.Set(colour);
                    UpdateEffects();
                };
                row.AddChild(picker);
                knob.Control = picker;
                return;
        }
    }

    // A tab's rows, three to a line across the bottom, made the first time a knob asks.
    private GridContainer TabRows(string tab)
    {
        TabContainer tabs = GetNode<TabContainer>("%Tabs");
        ScrollContainer? scroll = tabs.GetNodeOrNull<ScrollContainer>(tab);

        if (scroll == null)
        {
            scroll = new ScrollContainer { Name = tab, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            tabs.AddChild(scroll);
            GridContainer rows = new GridContainer { Name = "Rows", Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            rows.AddThemeConstantOverride("h_separation", 24);
            rows.AddThemeConstantOverride("v_separation", 2);
            scroll.AddChild(rows);
        }

        return scroll.GetNode<GridContainer>("Rows");
    }

    // Each control set to its knob's value now, without turning anything.
    private void ShowValues()
    {
        foreach (DevKnob knob in _knobs)
        {
            Variant value = knob.Get();

            switch (knob.Kind)
            {
                case DevKnobKind.Toggle:
                    ((CheckBox)knob.Control!).SetPressedNoSignal(value.AsBool());
                    break;
                case DevKnobKind.Slider:
                    ((HSlider)knob.Control!).SetValueNoSignal(value.AsSingle());
                    knob.Shown!.Text = Number(value.AsSingle());
                    break;
                case DevKnobKind.Choice:
                    ((OptionButton)knob.Control!).Select(value.AsInt32());
                    break;
                case DevKnobKind.Color:
                    ((ColorPickerButton)knob.Control!).Color = value.AsColor();
                    break;
            }
        }
    }

    // The values Reset goes back to; the world's again when the world is new.
    private void CaptureDefaults(bool worldOnly)
    {
        foreach (DevKnob knob in _knobs)
        {
            if (!worldOnly || knob.OnWorld)
            {
                knob.Default = knob.Get();
            }
        }
    }

    private void Reset()
    {
        // The hour is the game's clock, not a look: Reset lets go of it and leaves it.
        _holdHour = false;

        foreach (DevKnob knob in _knobs)
        {
            if (knob.Key != "hour" && knob.Key != "hold_hour")
            {
                knob.Set(knob.Default);
            }
        }

        Camera3D? camera = Camera();

        if (camera != null)
        {
            camera.Attributes = _cameraWas;
        }

        UpdateEffects();
        ShowValues();
        Status("Reset to how the game started.");
    }

    // Every value, and what changed from the start, as JSON; the screenshot a few frames
    // later, once the panel is out of the picture.
    private void Save()
    {
        string folder = ProjectSettings.GlobalizePath(SaveFolder);
        DirAccess.MakeDirRecursiveAbsolute(folder);
        string stamp = Time.GetDatetimeStringFromSystem().Replace(":", "-");
        Godot.Collections.Dictionary all = new Godot.Collections.Dictionary();
        Godot.Collections.Dictionary changed = new Godot.Collections.Dictionary();

        foreach (DevKnob knob in _knobs)
        {
            Variant now = ForJson(knob, knob.Get());
            Variant was = ForJson(knob, knob.Default);
            all[knob.Key] = now;

            if (Json.Stringify(now) != Json.Stringify(was))
            {
                changed[knob.Key] = new Godot.Collections.Dictionary { { "was", was }, { "now", now } };
            }
        }

        Godot.Collections.Dictionary file = new Godot.Collections.Dictionary
        {
            { "saved", stamp },
            { "screenshot", stamp + ".png" },
            { "changed", changed },
            { "all", all },
        };

        string jsonPath = folder + "/" + stamp + ".json";
        using (FileAccess? json = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Write))
        {
            json?.StoreString(Json.Stringify(file, "  "));
        }

        _shotPath = folder + "/" + stamp + ".png";
        _shotIn = 3;
        Visible = false;
        Status("Saved " + changed.Count + " changes: graphics-saves/" + stamp + ".json");
        GD.Print("Graphics panel: saved " + jsonPath);
    }

    // Choices by name and colours as hex, so the file reads without the panel.
    private static Variant ForJson(DevKnob knob, Variant value)
    {
        switch (knob.Kind)
        {
            case DevKnobKind.Choice:
                int index = value.AsInt32();
                return index >= 0 && index < knob.Choices.Length ? knob.Choices[index] : index.ToString();
            case DevKnobKind.Color:
                return "#" + value.AsColor().ToHtml();
            case DevKnobKind.Slider:
                return Math.Round(value.AsSingle(), 4);
            default:
                return value;
        }
    }

    private void FindWorld()
    {
        Node3D? world = GetTree().Root.FindChild("World", true, false) as Node3D;

        if (world == _world)
        {
            return;
        }

        _world = world;
        _cameraWas = Camera()?.Attributes;
        CaptureDefaults(true);
        ShowValues();
        Status(world == null ? "Not in the world yet: only Edges, Pixels and Screen apply." : "");
    }

    private void Status(string text)
    {
        GetNode<Label>("%Status").Text = text;
    }

    private Godot.Environment? Env()
    {
        return _world?.GetNodeOrNull<WorldEnvironment>("Environment")?.Environment;
    }

    private DirectionalLight3D? Sun()
    {
        return _world?.GetNodeOrNull<DirectionalLight3D>("Sun");
    }

    private Camera3D? Camera()
    {
        return _world?.GetNodeOrNull<Camera3D>("Camera");
    }

    private DayNight? DayNight()
    {
        return _world?.GetNodeOrNull<DayNight>("DayNight");
    }

    // The game's hour now, from its clock ("17:30" is 17.5); noon before it is known.
    private float ClockHour()
    {
        string clock = DayNight()?.ClockText ?? "";
        string[] parts = clock.Split(':');
        int hours;
        int minutes;

        if (parts.Length == 2 && int.TryParse(parts[0], out hours) && int.TryParse(parts[1], out minutes))
        {
            return hours + (minutes / 60f);
        }

        return 12f;
    }

    private void WithEnv(Action<Godot.Environment> change)
    {
        Godot.Environment? env = Env();

        if (env != null)
        {
            change(env);
        }
    }

    private void WithSun(Action<DirectionalLight3D> change)
    {
        DirectionalLight3D? sun = Sun();

        if (sun != null)
        {
            change(sun);
        }
    }

    private void WithCamera(Action<Camera3D> change)
    {
        Camera3D? camera = Camera();

        if (camera != null)
        {
            change(camera);
        }
    }

    // The blur is the panel's own camera look, put on the camera once a blur knob moves.
    private void WithLook(Action<CameraAttributesPractical> change)
    {
        change(_cameraLook);
        WithCamera(camera => camera.Attributes = _cameraLook);
    }

    private Variant Effect(string name)
    {
        ShaderMaterial? material = Effects?.Material as ShaderMaterial;
        return material == null ? default : material.GetShaderParameter(name);
    }

    private void SetEffect(string name, Variant value)
    {
        (Effects?.Material as ShaderMaterial)?.SetShaderParameter(name, value);
    }

    // The effects layer is drawn only while some effect is on.
    private void UpdateEffects()
    {
        if (Effects == null)
        {
            return;
        }

        Effects.Visible = Effect("pixelate").AsBool() || Effect("posterize").AsBool()
            || Effect("vignette").AsSingle() > 0f || Effect("grain").AsSingle() > 0f
            || Effect("fringe").AsSingle() > 0f || Effect("scanlines").AsSingle() > 0f
            || Effect("sharpen").AsSingle() > 0f || Effect("tint_amount").AsSingle() > 0f;
    }

    private static string Number(float value)
    {
        return Math.Abs(value) < 0.1f && value != 0f ? value.ToString("0.0000") : value.ToString("0.##");
    }
}
