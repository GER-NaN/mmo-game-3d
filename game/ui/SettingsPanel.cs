namespace MmoGame3d.Ui;

using System;
using Godot;
using MmoGame3d.Client;

// Changes apply at once and are saved on Back, so what you see is what is kept.
public partial class SettingsPanel : Control
{
    public event Action? Closed;

    private ClientSettings _settings = null!;

    public void Open(ClientSettings settings)
    {
        _settings = settings;
        CheckBox fullscreen = GetNode<CheckBox>("%Fullscreen");
        fullscreen.ButtonPressed = settings.Fullscreen;
        fullscreen.Toggled += OnFullscreenToggled;
        GetNode<Button>("%Back").Pressed += OnBack;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            OnBack();
        }
    }

    private void OnFullscreenToggled(bool on)
    {
        _settings.Fullscreen = on;
        _settings.Apply();
    }

    private void OnBack()
    {
        _settings.Save();
        Closed?.Invoke();
        QueueFree();
    }
}
