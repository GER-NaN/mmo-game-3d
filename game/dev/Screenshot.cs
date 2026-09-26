namespace MmoGame3d.Dev;

using Godot;

/// <summary>
/// Saves what the window shows to a PNG some seconds after entering the world, then
/// quits, so a change can be looked at without anyone driving the game. With overview,
/// the camera looks down on the whole zone from above instead of following the player.
/// Needs a real window: a headless client draws nothing.
/// </summary>
public partial class Screenshot : Node
{
    private string _path = "";
    private double _delay;
    private bool _overview;
    private double _elapsed;
    private bool _taken;

    public void Start(string path, bool overview, double delaySeconds)
    {
        _path = path;
        _overview = overview;
        _delay = delaySeconds;
    }

    public override void _Process(double delta)
    {
        Camera3D? camera = GetViewport().GetCamera3D();

        if (camera == null || _taken)
        {
            return;
        }

        if (_overview)
        {
            camera.SetProcess(false);
            camera.Position = new Vector3(0f, 95f, 55f);
            camera.LookAt(new Vector3(0f, 0f, 2f), Vector3.Up);
        }

        _elapsed += delta;

        if (_elapsed < _delay)
        {
            return;
        }

        _taken = true;
        Image image = GetViewport().GetTexture().GetImage();
        Error error = image.SavePng(_path);
        GD.Print(error == Error.Ok ? "Screenshot saved to " + _path : "Screenshot failed: " + error);
        GetTree().Quit();
    }
}
