namespace MmoGame3d.Tests.Bots;

using System.Collections.Generic;
using System.Numerics;
using MmoGame3d.BotJudging;

// The bots' judges must catch a bot that does not do what it set out to do, and stay quiet
// about a bot doing something odd on purpose. Each test hands a check a run of what a bot
// saw; the real tracks come from findings of 2026-09-27 (docs/engineering/bot-testing-findings.md).
public class JudgeTests
{
    // ---------------------------------------------------------------- going somewhere

    [Fact]
    public void ATravellerThatStandsStillIsCaught()
    {
        // Told to go to the college, and standing in town: stuck, once long enough.
        TravelWatch watch = new TravelWatch("college");
        string? verdict = null;

        for (double t = 0; t <= TravelWatch.StillFor + 1; t += 0.5)
        {
            verdict = watch.Look(0.5, "town", new Vector3(10f, 0f, 5f));

            if (t < TravelWatch.StillFor - 1)
            {
                Assert.Null(verdict);
            }
        }

        Assert.NotNull(verdict);
        Assert.Contains("on the way to college", verdict);
    }

    [Fact]
    public void ATravellerOnTheMoveIsNotStuck()
    {
        TravelWatch watch = new TravelWatch("college");

        for (int i = 0; i < 120; i++)
        {
            Assert.Null(watch.Look(0.5, "town", new Vector3(i * 2f, 0f, 0f)));
        }
    }

    [Fact]
    public void AZoneChangeOnTheWayStartsTheClockAgain()
    {
        TravelWatch watch = new TravelWatch("greenhouse");

        for (int i = 0; i < 40; i++)
        {
            Assert.Null(watch.Look(0.5, "town", Vector3.Zero));
        }

        for (int i = 0; i < 40; i++)
        {
            Assert.Null(watch.Look(0.5, "outskirts", Vector3.Zero));
        }
    }

    [Fact]
    public void ABotWalkingOnTheSpotForHalfAMinuteIsStuck()
    {
        List<TrackSample> history = Looks(32, 2, t => new Vector3(4f, 0f, 4f), walking: true);

        Assert.True(Stuck.Judge(history, new Vector3(4f, 0f, 4f), 32));
    }

    [Fact]
    public void StandingStillAtATerminalIsNotStuck()
    {
        List<TrackSample> history = Looks(32, 2, t => new Vector3(4f, 0f, 4f), walking: false);

        Assert.False(Stuck.Judge(history, new Vector3(4f, 0f, 4f), 32));
    }

    [Fact]
    public void AWalkThatGetsSomewhereIsNotStuck()
    {
        List<TrackSample> history = Looks(32, 2, t => new Vector3((float)t, 0f, 0f), walking: true);

        Assert.False(Stuck.Judge(history, new Vector3(32f, 0f, 0f), 32));
    }

    [Fact]
    public void TwentySecondsOnTheSpotIsNotYetStuck()
    {
        List<TrackSample> history = Looks(20, 2, t => Vector3.Zero, walking: true);

        Assert.False(Stuck.Judge(history, Vector3.Zero, 20));
    }

    // ---------------------------------------------------------------- back and forth

    [Fact]
    public void TheServerPuttingTheBodyBackIsThrashing()
    {
        // Soak7, just after login, holding only forward: the client ran ahead and was
        // snapped back again and again (the lost walks, fixed that night).
        double[][] track =
        {
            new[] { 4.78, 6.75, 47.89 }, new[] { 4.52, 6.75, 47.89 }, new[] { 4.25, 6.75, 47.89 }, new[] { 3.98, 6.75, 47.89 },
            new[] { 3.72, 6.75, 47.89 }, new[] { 3.45, 6.75, 47.89 }, new[] { 3.18, 6.75, 47.89 }, new[] { 2.92, 6.75, 47.89 },
            new[] { 2.67, 6.75, 47.89 }, new[] { 2.4, 6.75, 47.89 }, new[] { 2.13, 6.75, 47.89 }, new[] { 1.87, 6.75, 47.89 },
            new[] { 1.6, 7.32, 46.71 }, new[] { 1.33, 7.43, 45.38 }, new[] { 1.07, 6.82, 46.97 }, new[] { 0.8, 6.94, 45.64 },
            new[] { 0.53, 6.8, 47.3 }, new[] { 0.27, 6.91, 45.98 }, new[] { 0, 6.77, 47.64 },
        };

        Assert.NotNull(Thrashing.Judge(Recorded(track)));
    }

    [Fact]
    public void AnotherBodyPutBackAfterLoginIsThrashing()
    {
        // Soak5 at login, 02:57: the packet throttle at 0 dropped every walk.
        double[][] track =
        {
            new[] { 4.75, 30.14, 3.94 }, new[] { 4.5, 30.14, 3.94 }, new[] { 4.23, 30.14, 3.94 }, new[] { 3.97, 30.14, 3.94 },
            new[] { 3.71, 30.14, 3.94 }, new[] { 3.44, 29.32, 3.89 }, new[] { 3.18, 28.22, 3.15 }, new[] { 2.92, 29.7, 3.57 },
            new[] { 2.66, 28.68, 2.73 }, new[] { 2.4, 30.01, 3.85 }, new[] { 2.13, 28.91, 3.08 }, new[] { 1.86, 27.83, 2.32 },
            new[] { 1.6, 29.27, 3.33 }, new[] { 1.33, 28.17, 2.56 }, new[] { 1.07, 29.6, 3.56 }, new[] { 0.8, 28.51, 2.79 },
            new[] { 0.53, 29.87, 3.75 }, new[] { 0.27, 28.78, 2.99 }, new[] { 0, 30.14, 3.94 },
        };

        Assert.NotNull(Thrashing.Judge(Recorded(track)));
    }

    [Fact]
    public void AWedgedBodyJitteringAsItTurnsIsNotThrashing()
    {
        // Soak5, the wedger, turning in place in a gap: a tenth of a metre each look. It
        // was reported until moves counted from 1 m/s.
        double[][] track =
        {
            new[] { 4.8, -13.35, 21.28 }, new[] { 4.53, -14.06, 21.53 }, new[] { 4.27, -13.98, 21.47 }, new[] { 4.0, -14.02, 21.44 },
            new[] { 3.73, -14.05, 21.5 }, new[] { 3.47, -14.02, 21.48 }, new[] { 3.2, -14.0, 21.56 }, new[] { 2.93, -13.93, 21.44 },
            new[] { 2.67, -13.93, 21.51 }, new[] { 2.4, -13.99, 21.56 }, new[] { 2.13, -14.05, 21.51 }, new[] { 1.87, -14.04, 21.5 },
            new[] { 1.6, -13.94, 21.43 }, new[] { 1.33, -14.01, 21.43 }, new[] { 1.07, -14.05, 21.49 }, new[] { 0.8, -14.01, 21.57 },
            new[] { 0.53, -14.0, 21.55 }, new[] { 0.27, -13.98, 21.49 }, new[] { 0, -13.97, 21.48 },
        };

        Assert.Null(Thrashing.Judge(Recorded(track)));
    }

    [Fact]
    public void BobbingUpAndDownAgainstAWallIsNotThrashing()
    {
        // The escaper at the world's edge: a metre up and down, nowhere across the ground.
        List<TrackSample> track = Looks(5, Thrashing.Every, t => new Vector3(49.5f, (int)(t / Thrashing.Every) % 2 == 0 ? 0f : 1f, -20f), walking: true);

        Assert.Null(Thrashing.Judge(track));
    }

    [Fact]
    public void WalkingInACircleIsNotThrashing()
    {
        List<TrackSample> track = Looks(5, Thrashing.Every, t => new Vector3((float)System.Math.Cos(t) * 3f, 0f, (float)System.Math.Sin(t) * 3f), walking: true);

        Assert.Null(Thrashing.Judge(track));
    }

    [Fact]
    public void WalkingStraightIsNotThrashing()
    {
        List<TrackSample> track = Looks(5, Thrashing.Every, t => new Vector3((float)t * 4f, 0f, 0f), walking: true);

        Assert.Null(Thrashing.Judge(track));
    }

    // ---------------------------------------------------------------- zone changes

    [Fact]
    public void BouncingStraightBackThroughADoorIsPingPong()
    {
        ZoneChanges changes = new ZoneChanges();
        changes.Add(0, "town", "shop");
        changes.Add(2, "shop", "town");
        changes.Add(4, "town", "shop");
        changes.Add(6, "shop", "town");

        Assert.True(changes.PingPonging);
    }

    [Fact]
    public void VisitsWithTimeInEachPlaceAreNotPingPong()
    {
        ZoneChanges changes = new ZoneChanges();
        changes.Add(0, "town", "shop");
        changes.Add(20, "shop", "town");
        changes.Add(25, "town", "college");
        changes.Add(45, "college", "town");

        Assert.False(changes.PingPonging);
        Assert.False(changes.Churning);
    }

    [Fact]
    public void NineZoneChangesInAMinuteAreChurn()
    {
        ZoneChanges changes = new ZoneChanges();
        string[] zones = { "town", "shop", "town", "college", "town", "subway", "town", "outskirts", "greenhouse", "outskirts" };

        for (int i = 1; i < zones.Length; i++)
        {
            changes.Add(i * 5, zones[i - 1], zones[i]);
        }

        Assert.True(changes.Churning);
    }

    // ---------------------------------------------------------------- emotes

    [Fact]
    public void AnEmoteThatNeverShowsIsCaught()
    {
        Assert.NotNull(EmoteCheck.Judge("wave", clear: true, online: false, seen: false));
    }

    [Fact]
    public void AnEmoteThatShowsIsFine()
    {
        Assert.Null(EmoteCheck.Judge("wave", clear: true, online: false, seen: true));
    }

    [Fact]
    public void AnEmoteWhileOnlineMustNotShow()
    {
        Assert.NotNull(EmoteCheck.Judge("sit", clear: false, online: true, seen: true));
        Assert.Null(EmoteCheck.Judge("sit", clear: false, online: true, seen: false));
    }

    [Fact]
    public void WithAScreenOpenTheEmoteIsNotJudged()
    {
        Assert.Null(EmoteCheck.Judge("cheer", clear: false, online: false, seen: false));
    }

    // Looks every step seconds for this long, the body where place puts it.
    private static List<TrackSample> Looks(double seconds, double step, System.Func<double, Vector3> place, bool walking)
    {
        List<TrackSample> looks = new List<TrackSample>();

        for (double t = 0; t <= seconds + 0.0001; t += step)
        {
            looks.Add(new TrackSample(t, place(t), walking));
        }

        return looks;
    }

    // A finding's track: seconds ago, x, z; oldest first.
    private static List<TrackSample> Recorded(double[][] rows)
    {
        List<TrackSample> track = new List<TrackSample>();

        foreach (double[] row in rows)
        {
            track.Add(new TrackSample(5 - row[0], new Vector3((float)row[1], 0f, (float)row[2]), true));
        }

        return track;
    }
}
