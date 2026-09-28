namespace MmoGame3d.Ui;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Client;
using MmoGame3d.Rules.Terminals;

/// <summary>
/// Agent Defense as it is played: four lanes, cues coming down, a line to hit them on,
/// the defense_lane actions (D F J K). It plays the chart of the seed the server sent,
/// keeps every press, and hands them over at the end; the server scores them
/// (AgentDefense.Score). The score shown while playing is the same sum, worked out here
/// as a preview.
/// </summary>
public partial class AgentDefenseView : Control
{
    // Input actions in project.godot (D F J K), shared with the world's keys: the terminal
    // takes the keys while it is open.
    public static readonly string[] LaneActions = { "defense_lane_1", "defense_lane_2", "defense_lane_3", "defense_lane_4" };

    // Placeholders: how far ahead cues show, and the count-in before the first.
    private const int AheadMs = 1800;
    private const int CountInMs = 3000;

    private static readonly Color Back = new Color(0.03f, 0.05f, 0.07f);
    private static readonly Color LaneLine = new Color(0.2f, 0.35f, 0.4f);
    private static readonly Color Cue = new Color(0.4f, 0.9f, 1f);
    private static readonly Color HitLine = new Color(1f, 0.85f, 0.3f);

    private List<DefenseCue> _chart = new List<DefenseCue>();
    private readonly List<DefensePress> _presses = new List<DefensePress>();
    private readonly double[] _flash = new double[AgentDefense.Lanes];
    private ulong _startedAt;
    private bool _playing;
    private int _lengthMs = AgentDefense.LengthMs;
    private DefenseResult _preview = new DefenseResult();

    // The presses, packed, when the run is over.
    public event Action<int[]>? Finished;

    public bool Playing
    {
        get { return _playing; }
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        ClipContents = true;
    }

    public void Play(int seed, int lengthMs)
    {
        _lengthMs = lengthMs;
        _chart = AgentDefense.Chart(seed, lengthMs);
        _presses.Clear();
        _preview = new DefenseResult { Cues = _chart.Count };
        _startedAt = Time.GetTicksMsec() + CountInMs;
        _playing = true;
        GrabFocus();
    }

    // Ms since the first cue's clock started; negative during the count-in.
    private int Now()
    {
        return (int)((long)Time.GetTicksMsec() - (long)_startedAt);
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < _flash.Length; i++)
        {
            _flash[i] = Math.Max(0, _flash[i] - delta);
        }

        if (_playing && Now() > _lengthMs + 500)
        {
            _playing = false;
            int[] packed = new int[_presses.Count];

            for (int i = 0; i < packed.Length; i++)
            {
                packed[i] = AgentDefense.Pack(_presses[i]);
            }

            Finished?.Invoke(packed);
        }

        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_playing || !@event.IsPressed() || @event.IsEcho())
        {
            return;
        }

        int lane = -1;

        for (int i = 0; i < LaneActions.Length; i++)
        {
            if (@event.IsAction(LaneActions[i]))
            {
                lane = i;
            }
        }

        if (lane < 0)
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        int at = Now();

        if (at < 0)
        {
            return;
        }

        _presses.Add(new DefensePress(at, lane));
        Audio.AudioDirector.Current?.Play("term.hit");
        _flash[lane] = 0.12;
        _preview = AgentDefense.Score(_chart, _presses);
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), Back);
        float laneWidth = Size.X / AgentDefense.Lanes;
        float hitY = Size.Y - 60f;
        int now = Now();

        for (int lane = 0; lane < AgentDefense.Lanes; lane++)
        {
            float x = lane * laneWidth;
            DrawRect(new Rect2(x, 0, laneWidth, Size.Y), new Color(Cue, (float)(_flash[lane] * 2.5)));
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), LaneLine, 1f);
            DrawString(ThemeDB.FallbackFont, new Vector2(x + (laneWidth / 2f) - 6f, Size.Y - 20f), ClientSettings.KeyName(LaneActions[lane]), HorizontalAlignment.Left, -1, 20, HitLine);
        }

        DrawLine(new Vector2(0, hitY), new Vector2(Size.X, hitY), HitLine, 3f);

        foreach (DefenseCue cue in _chart)
        {
            int until = cue.AtMs - now;

            if (until < -200 || until > AheadMs)
            {
                continue;
            }

            float y = hitY - (until / (float)AheadMs * hitY);
            DrawRect(new Rect2((cue.Lane * laneWidth) + 8f, y - 8f, laneWidth - 16f, 16f), Cue);
        }

        string status;

        if (!_playing)
        {
            status = _chart.Count == 0 ? "" : "Done. Sending to the grid...";
        }
        else if (now < 0)
        {
            status = "Get ready: " + ((-now / 1000) + 1);
        }
        else
        {
            status = _preview.Points + " points   combo " + _preview.BestCombo + "   " + Math.Max(0, (_lengthMs - now) / 1000) + " s";
        }

        DrawString(ThemeDB.FallbackFont, new Vector2(12f, 28f), status, HorizontalAlignment.Left, -1, 20, HitLine);
    }
}
