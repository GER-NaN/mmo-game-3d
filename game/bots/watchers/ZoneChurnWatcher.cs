namespace MmoGame3d.Bots;

using System.Collections.Generic;
using MmoGame3d.Zones;

/// <summary>
/// Doors that throw the player about: several stays of only moments in a zone within a
/// minute, as when a door's trigger reaches over an arrival spot and sends the player
/// straight back. Many doors in a row is not it by itself; a bot touring rooms does that.
/// </summary>
public class ZoneChurnWatcher : BotWatcher
{
    // No player means to be in a zone for less than this: a door throwing one back is near
    // instant, while going in and straight out again (a tour of rooms) takes seconds.
    private const double ShortStay = 1.5;
    private const int ShortStaysInAMinute = 3;

    private readonly List<double> _shortStays = new List<double>();
    private readonly List<string> _shortZones = new List<string>();
    private string _zoneId = "";
    private double _arrivedAt;
    private double _clock;

    public ZoneChurnWatcher()
        : base("zone-churn", false)
    {
    }

    public override string? Look(BotBody body, BotStep? step, double delta)
    {
        _clock += delta;
        Zone? zone = body.Zone;

        if (zone == null || zone.ZoneId == _zoneId)
        {
            return null;
        }

        if (_zoneId.Length > 0 && _clock - _arrivedAt < ShortStay)
        {
            _shortStays.Add(_clock);
            _shortZones.Add(_zoneId);
        }

        _zoneId = zone.ZoneId;
        _arrivedAt = _clock;

        while (_shortStays.Count > 0 && _clock - _shortStays[0] > 60)
        {
            _shortStays.RemoveAt(0);
            _shortZones.RemoveAt(0);
        }

        if (_shortStays.Count < ShortStaysInAMinute)
        {
            return null;
        }

        return _shortStays.Count + " stays under " + ShortStay + " s in a minute, in " + string.Join(", ", _shortZones);
    }
}
