namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Players;

// Changes apply at once and are saved on Back, so what you see is what is kept.
public partial class SettingsPanel : Control
{
    private static readonly Dictionary<string, string> ActionNames = new Dictionary<string, string>
    {
        { "move_forward", "Walk forward" },
        { "move_back", "Walk back" },
        { "turn_left", "Turn left" },
        { "turn_right", "Turn right" },
        { "strafe_left", "Step left" },
        { "strafe_right", "Step right" },
        { "jump", "Jump" },
        { "interact", "Use" },
        { "phone", "Phone" },
        { "inventory", "Inventory" },
        { "map", "Map" },
        { "social", "Friends" },
        { "skills", "Skills" },
        { "emp", "EMP Emitter" },
    };

    private readonly Dictionary<string, Button> _keyButtons = new Dictionary<string, Button>();
    private ClientSettings _settings = null!;

    // The action waiting for its new key, or null.
    private string? _rebinding;

    public event Action? Closed;

    // Raised on every change, so the game applies it while the panel is open.
    public event Action? Changed;

    public void Open(ClientSettings settings)
    {
        _settings = settings;
        CheckBox fullscreen = GetNode<CheckBox>("%Fullscreen");
        fullscreen.ButtonPressed = settings.Fullscreen;
        fullscreen.Toggled += OnFullscreenToggled;

        HSlider sensitivity = GetNode<HSlider>("%Sensitivity");
        sensitivity.MinValue = ClientSettings.MinSensitivity;
        sensitivity.MaxValue = ClientSettings.MaxSensitivity;
        sensitivity.Value = settings.MouseSensitivity;
        sensitivity.ValueChanged += OnSensitivityChanged;

        HSlider distance = GetNode<HSlider>("%Distance");
        distance.MinValue = ChaseCamera.MinDistance;
        distance.MaxValue = ChaseCamera.MaxDistance;
        distance.Value = settings.CameraDistance;
        distance.ValueChanged += OnDistanceChanged;

        // Sound: a slider a bus, applied as it moves.
        VBoxContainer general = GetNode<VBoxContainer>("%General");
        general.AddChild(new Label { Text = "Sound" });

        foreach (string bus in ClientSettings.VolumeBuses)
        {
            string named = bus;
            HBoxContainer row = new HBoxContainer();
            row.AddChild(new Label { Text = bus, CustomMinimumSize = new Vector2(90, 0) });
            HSlider slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = settings.Volumes[bus], SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(140, 0) };
            slider.ValueChanged += value =>
            {
                _settings.Volumes[named] = (float)value;
                _settings.ApplyVolumes();
                _settings.Save();
            };
            row.AddChild(slider);
            general.AddChild(row);
        }

        GridContainer keys = GetNode<GridContainer>("%Keys");

        foreach (string action in ClientSettings.Rebindable)
        {
            string bound = action;
            keys.AddChild(new Label { Text = ActionNames[action] });
            Button button = new Button { CustomMinimumSize = new Vector2(120, 0) };
            button.Pressed += () => StartRebinding(bound);
            keys.AddChild(button);
            _keyButtons[action] = button;
        }

        GetNode<Button>("%ResetKeys").Pressed += OnResetKeys;
        Button back = GetNode<Button>("%Back");
        back.Pressed += OnBack;
        ShowValues();
    }

    // Before anything else sees the key: while waiting for one, every key is the answer,
    // Esc included, which cancels rather than closing the panel.
    public override void _Input(InputEvent @event)
    {
        InputEventKey? key = @event as InputEventKey;

        if (_rebinding == null || key == null || !key.Pressed || key.Echo)
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        if (key.Keycode != Key.Escape)
        {
            _settings.Bind(_rebinding, key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode);
            Changed?.Invoke();
        }

        _rebinding = null;
        ShowValues();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            OnBack();
        }
    }

    private void StartRebinding(string action)
    {
        _rebinding = action;
        ShowValues();
        _keyButtons[action].Text = "Press a key";
    }

    private void ShowValues()
    {
        GetNode<Label>("%SensitivityLabel").Text = "Mouse sensitivity   " + _settings.MouseSensitivity.ToString("0.00", CultureInfo.InvariantCulture);
        GetNode<Label>("%DistanceLabel").Text = "Camera distance   " + _settings.CameraDistance.ToString("0.0", CultureInfo.InvariantCulture) + " m";

        foreach (KeyValuePair<string, Button> entry in _keyButtons)
        {
            entry.Value.Text = ClientSettings.KeyName(entry.Key);
        }
    }

    private void OnFullscreenToggled(bool on)
    {
        _settings.Fullscreen = on;
        _settings.Apply();
    }

    private void OnSensitivityChanged(double value)
    {
        _settings.MouseSensitivity = (float)value;
        ShowValues();
        Changed?.Invoke();
    }

    private void OnDistanceChanged(double value)
    {
        _settings.CameraDistance = (float)value;
        ShowValues();
        Changed?.Invoke();
    }

    private void OnResetKeys()
    {
        _rebinding = null;
        _settings.ResetKeys();
        ShowValues();
        Changed?.Invoke();
    }

    private void OnBack()
    {
        _settings.Save();
        Closed?.Invoke();
        QueueFree();
    }
}
