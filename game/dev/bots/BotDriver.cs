namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Players;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Ui;

/// <summary>
/// Plays a client by itself (--bot), for long runs and headless checks. The rule, kept
/// from mmo-game: a bot may look things up, but it acts through input. It presses the
/// same keys and clicks the same buttons a person does, so everything past the keyboard
/// and mouse is the real game (BotBody).
///
/// The loop, the same for every persona: when free it picks an activity, a chain or an
/// aside (BotCatalog), weighted by the persona; rolls whether and when to cancel it or
/// lose the connection in it; routes to the activity's zone first (BotRouter); and runs
/// it frame by frame while its judge watches. A chain gives the next activity after each
/// one ends; an aside fires on its own timer too, pausing what runs. Between runs it
/// closes whatever is open, except after a cancel, which leaves everything as it is.
/// The persona is only numbers (BotPersona). Back at the main menu, BotKeeper takes over.
/// </summary>
public partial class BotDriver : Node
{
    // A person takes a moment to read before clicking.
    private const double JoinAfter = 0.8;

    // With nothing that can start, it looks again after this long.
    private const double PickAgain = 2;

    // An aside comes this many times as seldom while something else runs.
    private const double AsideBusyFactor = 3;

    // Findings for screens that would not close, each at most every five minutes.
    private const double CannotCloseRepeat = 300;

    private Random _random = new Random();
    private BotBody _body = null!;
    private BotPersona _persona = BotPersonas.Get(BotPersonas.Default);

    // What runs now: the activity (a travel first, when it happens elsewhere, with the
    // activity it is for), its judge, the chain it belongs to, and an aside pausing it.
    private BotActivity? _activity;
    private BotActivity? _after;
    private BotActivityJudge? _judgeOfActivity;
    private bool _needsMetAtStart;
    private BotChain? _chain;
    private BotActivity? _pendingInChain;
    private BotActivity? _aside;
    private BotActivityJudge? _judgeOfAside;
    private string _lastPicked = "";

    // Out of a trap the position judge saw: the escape, and what it interrupted.
    private bool _escaping;
    private BotActivity? _escapedFrom;

    // Walks that failed in a row: two, and it is trapped somewhere; it escapes first.
    private int _failedWalks;

    private readonly CloseAllStep _closer = new CloseAllStep();
    private bool _closing = true;
    private double _closingFor;
    private double _pickIn;

    // The interrupt rolled for the choice running now: seconds into it, or -1.
    private string _choice = "";
    private double _choiceFor;
    private double _cancelAt = -1;
    private double _cutAt = -1;

    // Seconds until each aside may fire again.
    private readonly Dictionary<BotActivity, double> _asideIn = new Dictionary<BotActivity, double>();

    private double _joinSeenFor;
    private bool _joinDecided;
    private bool _willJoin;
    private BotPositionJudge _judge = null!;
    private BotZoneJudge _zoneJudge = null!;
    private readonly BotErrorJudge _errorJudge = new BotErrorJudge();
    private readonly Dictionary<string, double> _cannotCloseAt = new Dictionary<string, double>();
    private readonly Dictionary<string, double> _reportedAt = new Dictionary<string, double>();
    private double _clock;
    private bool _outOfWorld = true;
    private bool _introduced;

    // Drops the connection as a lost one does, back to the main menu, where BotKeeper
    // logs in again. Set by ClientGame.
    public Action? CutConnection { get; set; }

    // Names the bot in the judge's findings and pictures.
    public string Profile { get; set; } = "";

    // Who it is (BotPersonas): what it likes to do, and at what pace.
    public string PersonaName { get; set; } = BotPersonas.Default;

    // Only these activities, chains or asides (--bot-only, comma-separated), again and
    // again, or "" for all of them.
    public string Only { get; set; } = "";

    // Every activity in turn, each to its end (--bot-everything; EverythingRun).
    public bool Everything { get; set; }

    public override void _Ready()
    {
        // Logged, so a run's choices can be drawn again (the world will differ).
        int seed = System.Environment.TickCount;
        _random = new Random(seed);
        GD.Print("Bot: seed " + seed);
        _body = new BotBody(this, _random);
        _persona = BotPersonas.Get(PersonaName);
        _body.Pace = _persona.Pace;
        _judge = new BotPositionJudge(Profile);
        _zoneJudge = new BotZoneJudge(Profile);
        _errorJudge.Start(Profile);
        _body.WalkFailed = step => _judge.WalkFailed(_body, Doing(), step);
        _body.Report = (kind, detail) =>
        {
            // The same thing seen again within five minutes is the same finding.
            double at;

            if (_reportedAt.TryGetValue(kind + detail, out at) && _clock - at < CannotCloseRepeat)
            {
                return;
            }

            _reportedAt[kind + detail] = _clock;
            Finding(kind, detail);
        };
        _closer.Begin(_body);
    }

    public override void _ExitTree()
    {
        _body?.Stop();
        _body?.Navigation.Clear();
        _errorJudge.Stop();
    }

    public override void _Process(double delta)
    {
        _body.Tick(delta);
        _clock += delta;

        // Not in the world: loading, or the menus, which are BotKeeper's.
        if (_body.Me == null)
        {
            _outOfWorld = true;
            return;
        }

        // Back in after a login: what the engine holds now, for a leak over many logins.
        if (_outOfWorld)
        {
            _outOfWorld = false;
            GD.Print("Bot: in the world; objects " + Performance.GetMonitor(Performance.Monitor.ObjectCount)
                + ", nodes " + Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)
                + ", orphan nodes " + Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)
                + ", resources " + Performance.GetMonitor(Performance.Monitor.ObjectResourceCount)
                + ", managed MB " + (GC.GetTotalMemory(false) / 1048576)
                + ", static MB " + (long)(Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1048576)
                + ", video MB " + (long)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576)
                + " (textures " + (long)(Performance.GetMonitor(Performance.Monitor.RenderTextureMemUsed) / 1048576)
                + ", buffers " + (long)(Performance.GetMonitor(Performance.Monitor.RenderBufferMemUsed) / 1048576) + ")");
        }

        if (!_introduced)
        {
            _introduced = true;
            Announce("I am " + ("aeiou".Contains(_persona.Name[0]) ? "an " : "a ") + _persona.Name + " bot: " + _persona.About);
        }

        AcceptInvites(delta);
        _body.Navigation.Tick(_body);
        string doing = Doing();
        BotStep? step = _aside != null ? _aside.Step : _activity?.Step;
        _judge.Tick(_body, delta, doing, step);
        _zoneJudge.Tick(_body, delta, doing, step != null && step.MovesZone);
        _errorJudge.Tick(_body, delta, doing, step);

        // Judged stuck (a wedge a walk never notices): out of it before anything else.
        if (_judge.IsStuck && !_escaping)
        {
            Escape();
            return;
        }

        if (_closing)
        {
            TickClosing(delta);
            return;
        }

        // Nothing breaks into an activity in the everything run: asides take their turn.
        if (_aside == null && !Everything)
        {
            TickAsideTimers(delta);
        }

        if (_aside != null)
        {
            TickAside(delta);
            return;
        }

        if (_activity == null)
        {
            _pickIn -= delta;

            if (_pickIn <= 0)
            {
                Pick();
            }

            return;
        }

        _choiceFor += delta;

        if (_cutAt >= 0 && _choiceFor >= _cutAt)
        {
            Cut();
            return;
        }

        if (_cancelAt >= 0 && _choiceFor >= _cancelAt)
        {
            CancelChoice();
            return;
        }

        TickActivity(delta);
    }

    // ---------------------------------------------------------------- choosing

    private void Pick()
    {
        if (_failedWalks >= 2)
        {
            _failedWalks = 0;
            Escape();
            return;
        }

        if (Everything)
        {
            PickEverything();
            return;
        }

        List<BotActivity> activities = new List<BotActivity>();
        List<BotActivity> asides = new List<BotActivity>();
        List<BotChain> chains = new List<BotChain>();
        List<BotActivity> all = new List<BotActivity>(BotCatalog.All.Activities);
        all.AddRange(_persona.Own);

        foreach (BotActivity activity in all)
        {
            if (Weight(activity.Weight, activity.Name) <= 0 || !Allowed(activity.Name) || !activity.CanStart(_body))
            {
                continue;
            }

            if (activity.Timing == BotTiming.Aside)
            {
                asides.Add(activity);
            }
            else if (activity.NeedsMet(_body) && Reachable(activity))
            {
                activities.Add(activity);
            }
        }

        foreach (BotChain chain in BotCatalog.All.Chains)
        {
            if (Weight(chain.Weight, chain.Name) > 0 && Allowed(chain.Name) && chain.CanStart(_body))
            {
                chains.Add(chain);
            }
        }

        // Not the same activity twice running, when there is a choice.
        if (activities.Count > 1)
        {
            activities.RemoveAll(a => a.Name == _lastPicked);
        }

        double activityShare = activities.Count > 0 ? _persona.ActivityShare : 0;
        double chainShare = chains.Count > 0 ? _persona.ChainShare : 0;
        double asideShare = asides.Count > 0 ? _persona.AsideShare : 0;
        double total = activityShare + chainShare + asideShare;

        if (total <= 0)
        {
            // Only one thing, and it cannot start here: back to town, where most things can.
            if (Only.Length > 0 && BotCatalog.BackToTown.CanStart(_body))
            {
                TakeActivity(BotCatalog.BackToTown);
                return;
            }

            _pickIn = PickAgain;
            return;
        }

        double roll = _random.NextDouble() * total;

        if (roll < activityShare)
        {
            TakeActivity(Weighted(activities, a => Weight(a.Weight, a.Name)));
        }
        else if (roll < activityShare + chainShare)
        {
            TakeChain(Weighted(chains, c => Weight(c.Weight, c.Name)));
        }
        else
        {
            StartAside(Weighted(asides, a => Weight(a.Weight, a.Name)));
        }
    }

    // The next of every activity: at once when its needs are met, after a goal that meets
    // them when not; skipped, with the reason, when it cannot start where it is done.
    private void PickEverything()
    {
        BotActivity next = EverythingRun.Next(_random);
        _choice = next.Name;
        _choiceFor = 0;
        _cancelAt = -1;
        _cutAt = -1;
        Announce("everything: " + next.Name);

        if (!next.NeedsMet(_body))
        {
            GoalChain goal = new GoalChain("everything: " + next.Name, 1, b => true, b => new List<BotFact>(next.Needs), 12, next.UsualSeconds * 4, b => next);
            _chain = goal;
            goal.Begin(_body, _random);
            GD.Print("Bot: goal chain \"" + goal.Name + "\" for its needs first");
            StartInChain(goal.Next(_body, null, BotEnd.Finished));
            return;
        }

        if (next.Zone.Length == 0 && !next.CanStart(_body))
        {
            GD.Print("Bot: \"" + next.Name + "\" cannot start here");
            EverythingRun.NotStarted("cannot start in " + _body.ZoneId);
            _pickIn = 0.5;
            return;
        }

        StartActivity(next);
    }

    // Its own weight times the persona's liking; one named in --bot-only is picked even
    // when only chains start it (weight 0).
    private double Weight(int weight, string name)
    {
        if (Only.Length > 0 && Allowed(name))
        {
            return Math.Max(1, weight);
        }

        return weight * _persona.Factor(name);
    }

    private T Weighted<T>(List<T> choices, Func<T, double> weight)
    {
        double total = 0;

        foreach (T choice in choices)
        {
            total += weight(choice);
        }

        double roll = _random.NextDouble() * total;

        foreach (T choice in choices)
        {
            roll -= weight(choice);

            if (roll < 0)
            {
                return choice;
            }
        }

        return choices[choices.Count - 1];
    }

    // Somewhere the router can take it from here (or here already).
    private bool Reachable(BotActivity activity)
    {
        return activity.Zone.Length == 0 || activity.Zone == _body.ZoneId || BotRouter.Route(_body.ZoneId, activity.Zone) != null;
    }

    private void TakeActivity(BotActivity activity)
    {
        _lastPicked = activity.Name;
        RollInterrupt(activity.Name, activity.UsualSeconds);
        Announce("wandering: " + activity.Name);
        StartActivity(activity);
    }

    private void TakeChain(BotChain chain)
    {
        _lastPicked = chain.Name;
        _chain = chain;
        chain.Begin(_body, _random);
        RollInterrupt(chain.Name, chain.UsualSeconds);
        GD.Print("Bot: " + chain.Kind.ToString().ToLowerInvariant() + " chain \"" + chain.Name + "\"");
        Announce((chain.Kind == ChainKind.Goal ? "goal: " : "chain: ") + chain.Name);

        // Its own business: out of any party first.
        BotActivity? first = chain.Solo && BotCatalog.LeaveParty.CanStart(_body) ? BotCatalog.LeaveParty : chain.Next(_body, null, BotEnd.Finished);
        StartInChain(first);
    }

    // Whether, and when, to walk away from this choice or lose the connection in it; the
    // log names the moment, so a replay can put it in the same place.
    private void RollInterrupt(string name, double usualSeconds)
    {
        _choice = name;
        _choiceFor = 0;
        _cancelAt = -1;
        _cutAt = -1;

        if (CutConnection != null && _random.NextDouble() < _persona.CutChance)
        {
            _cutAt = 1 + (_random.NextDouble() * 20 * _persona.Pace);
        }
        else if (_random.NextDouble() < _persona.CancelChance)
        {
            _cancelAt = 3 + (_random.NextDouble() * Math.Max(1, (usualSeconds * _persona.Pace) - 3));
        }

        GD.Print("Bot: taking \"" + name + "\""
            + (_cutAt >= 0 ? " (will lose the connection after " + (int)_cutAt + " s)" : "")
            + (_cancelAt >= 0 ? " (will cancel it after " + (int)_cancelAt + " s)" : ""));
    }

    // ---------------------------------------------------------------- running

    private void StartInChain(BotActivity? next)
    {
        // Links that cannot start here: skipped in a random chain, a finding in a related
        // one, where the author set up what each needs.
        while (next != null && _chain != null && !next.CanStart(_body))
        {
            GD.Print("Bot: \"" + next.Name + "\" cannot start here; next in \"" + _chain.Name + "\"");

            if (_chain.Kind == ChainKind.Related)
            {
                Finding("chain-step-cannot-start", "\"" + next.Name + "\" in \"" + _chain.Name + "\" cannot start in " + _body.ZoneId);
            }

            next = _chain.Next(_body, next, BotEnd.Failed);
        }

        if (next == null)
        {
            EndChain();
            return;
        }

        if (_chain != null && _chain.Kind == ChainKind.Goal)
        {
            Announce(_chain.Name + " -> " + next.Name);
        }

        StartActivity(next);
    }

    private void StartActivity(BotActivity activity)
    {
        // Somewhere else: the router first, then the activity.
        if (activity.Zone.Length > 0 && activity.Zone != _body.ZoneId)
        {
            _after = activity;
            activity = new TravelActivity(activity.Zone);
        }

        _activity = activity;
        _needsMetAtStart = activity.NeedsMet(_body);
        _judgeOfActivity = activity.NewJudge();
        _judgeOfActivity?.Before(_body);
        GD.Print("Bot: starting \"" + activity.Name + "\"" + (_after != null ? " for \"" + _after.Name + "\"" : ""));
        activity.Begin(_body);
    }

    private void TickActivity(double delta)
    {
        BotActivity activity = _activity!;
        string? wrong = _judgeOfActivity?.Watch(_body, delta);

        if (wrong != null)
        {
            activity.Cancel(_body, "judged");
            EndActivity(BotEnd.Failed, "judged: " + wrong, wrong);
            return;
        }

        StepResult result = activity.Tick(_body, delta);

        switch (result)
        {
            case StepResult.Running:
                return;
            case StepResult.Done:
                EndActivity(BotEnd.Finished, "done", null);
                return;
            default:
                EndActivity(BotEnd.Failed, activity.Why, null);
                return;
        }
    }

    // The activity is over: its judge's verdict, its promises, then what comes next.
    private void EndActivity(BotEnd end, string why, string? judged)
    {
        BotActivity done = _activity!;
        Judge(done, _judgeOfActivity, end, judged, _needsMetAtStart);
        GD.Print("Bot: " + (end == BotEnd.Finished ? "finished" : "gave up on") + " \"" + done.Name + "\": " + why);
        _failedWalks = end == BotEnd.Failed && done.FailedWalking ? _failedWalks + 1 : end == BotEnd.Finished ? 0 : _failedWalks;
        _activity = null;
        _judgeOfActivity = null;
        _body.Stop();

        // A travel for an activity: on to it; if the travel failed, so did the activity.
        if (_after != null)
        {
            BotActivity target = _after;
            _after = null;

            if (end == BotEnd.Finished)
            {
                StartActivity(target);
                return;
            }

            done = target;
        }

        if (_escaping)
        {
            _escaping = false;

            if (_escapedFrom != null)
            {
                done = _escapedFrom;
                end = BotEnd.Failed;
                _escapedFrom = null;
            }
        }

        if (Everything && done == EverythingRun.Current)
        {
            EverythingRun.Ended(end == BotEnd.Finished, why);
        }

        if (_chain != null)
        {
            // Tidied between links, then the next.
            _pendingInChain = _chain.Next(_body, done, end);

            if (_pendingInChain == null)
            {
                EndChain();
                return;
            }
        }
        else
        {
            _choice = "";
        }

        StartClosing();
    }

    // needsMet: whether its needs held when it started. Started without them (a buy in
    // a random chain with too little money), a refusal is the right answer, not a broken
    // promise.
    private void Judge(BotActivity activity, BotActivityJudge? judge, BotEnd end, string? judged, bool needsMet = true)
    {
        string? verdict = judged ?? judge?.After(_body, end);

        if (verdict != null)
        {
            Finding("activity-failed", activity.Name + ": " + verdict);
        }

        if (end != BotEnd.Finished || !needsMet)
        {
            return;
        }

        foreach (BotFact promise in activity.Gives)
        {
            // Money promised is more money; the amount is the goal's business.
            if (promise.Kind != FactKind.MoneyAtLeast && !promise.IsTrue(_body))
            {
                Finding("promise-broken", activity.Name + " finished, but not " + promise);
            }
        }
    }

    private void EndChain()
    {
        BotChain? chain = _chain;
        _chain = null;
        _pendingInChain = null;
        _choice = "";
        _cancelAt = -1;
        _cutAt = -1;

        // A goal for an activity's needs that ended without getting to it.
        if (Everything && EverythingRun.Current != null)
        {
            EverythingRun.Ended(false, "its needs were not met" + (chain != null && chain.Why.Length > 0 ? ": " + chain.Why : ""));
        }

        if (chain != null)
        {
            GD.Print("Bot: chain \"" + chain.Name + "\" over" + (chain.Why.Length > 0 ? " (" + chain.Why + ")" : "") + ": " + chain.Describe());
            Announce(chain.Why.Length > 0 ? "giving up " + chain.Name + ": " + chain.Why : "done: " + chain.Name);
        }

        StartClosing();
    }

    private void StartClosing()
    {
        _closing = true;
        _closingFor = 0;
        _closer.Begin(_body);
    }

    private void TickClosing(double delta)
    {
        _closingFor += delta;

        if (_closer.Tick(_body, delta) == StepResult.Running && _closingFor <= _closer.Limit)
        {
            return;
        }

        _closing = false;

        if (_closingFor > _closer.Limit)
        {
            CouldNotClose();
        }

        if (_pendingInChain != null)
        {
            BotActivity next = _pendingInChain;
            _pendingInChain = null;
            StartInChain(next);
            return;
        }

        // A moment between one thing and the next, longer at a slower pace.
        _pickIn = (0.5 + _random.NextDouble()) * _persona.Pace;
    }

    // ---------------------------------------------------------------- asides

    private void TickAsideTimers(double delta)
    {
        List<BotActivity> all = new List<BotActivity>(BotCatalog.All.Activities);
        all.AddRange(_persona.Own);
        bool busy = _activity != null;

        foreach (BotActivity aside in all)
        {
            if (aside.Timing != BotTiming.Aside || _persona.Factor(aside.Name) <= 0 || !Allowed(aside.Name))
            {
                continue;
            }

            double left;

            if (!_asideIn.TryGetValue(aside, out left))
            {
                left = NextAsideIn(aside, busy);
            }

            left -= delta;

            if (left > 0)
            {
                _asideIn[aside] = left;
                continue;
            }

            _asideIn[aside] = NextAsideIn(aside, busy);

            if (aside.CanStart(_body) && (_activity == null || _activity.AllowsAsides))
            {
                StartAside(aside);
                return;
            }
        }
    }

    // Seconds to the next firing: random, around the aside's own mean, scaled by the
    // persona and by being busy.
    private double NextAsideIn(BotActivity aside, double busy)
    {
        double mean = aside.AsideEvery / Math.Max(0.01, _persona.AsideRate) * busy;
        return -Math.Log(1 - _random.NextDouble()) * mean;
    }

    private double NextAsideIn(BotActivity aside, bool busy)
    {
        return NextAsideIn(aside, busy ? AsideBusyFactor : 1);
    }

    private void StartAside(BotActivity aside)
    {
        _aside = aside;
        _judgeOfAside = aside.NewJudge();
        _judgeOfAside?.Before(_body);
        GD.Print("Bot: aside \"" + aside.Name + "\"" + (_activity != null ? " during \"" + _activity.Name + "\"" : ""));
        _body.Stop();
        aside.Begin(_body);
    }

    private void TickAside(double delta)
    {
        BotActivity aside = _aside!;
        string? wrong = _judgeOfAside?.Watch(_body, delta);
        StepResult result = wrong != null ? StepResult.Failed : aside.Tick(_body, delta);

        if (result == StepResult.Running)
        {
            return;
        }

        BotEnd end = result == StepResult.Done ? BotEnd.Finished : BotEnd.Failed;
        Judge(aside, _judgeOfAside, end, wrong);
        GD.Print("Bot: aside \"" + aside.Name + "\" " + (end == BotEnd.Finished ? "done" : "failed: " + (wrong ?? aside.Why)));
        _aside = null;
        _judgeOfAside = null;
        _body.Stop();

        // Taken when free: a moment, then the next pick. Mid-activity, it carries on.
        if (_activity == null && !_closing)
        {
            _pickIn = (0.5 + _random.NextDouble()) * _persona.Pace;
        }
    }

    // ---------------------------------------------------------------- interrupts

    // Walks away mid-step, leaving everything as it is: the state a distracted player
    // leaves. The chain goes too.
    private void CancelChoice()
    {
        GD.Print("Bot: cancelling \"" + _choice + "\" after " + (int)_choiceFor + " s, during \"" + (_activity?.Name ?? "") + "\" at \""
            + (_activity?.Step?.Name ?? "") + "\"");
        Announce("cancelling " + _choice + " (mid " + (_activity?.Name ?? "nothing") + ")");
        StopEverything("cancelled");
        _pickIn = 0;
    }

    // The connection goes mid-step, with whatever is open left open; BotKeeper logs in
    // again.
    private void Cut()
    {
        GD.Print("Bot: cutting the connection after " + (int)_choiceFor + " s of \"" + _choice + "\", during \"" + (_activity?.Name ?? "") + "\" at \""
            + (_activity?.Step?.Name ?? "") + "\"");
        StopEverything("connection cut");
        StartClosing();
        CutConnection?.Invoke();
    }

    private void StopEverything(string reason)
    {
        if (_aside != null)
        {
            _aside.Cancel(_body, reason);
            Judge(_aside, _judgeOfAside, BotEnd.Cancelled, null);
            _aside = null;
            _judgeOfAside = null;
        }

        if (_activity != null)
        {
            _activity.Cancel(_body, reason);
            Judge(_activity, _judgeOfActivity, BotEnd.Cancelled, null);
            _activity = null;
            _judgeOfActivity = null;
        }

        _after = null;
        _chain = null;
        _pendingInChain = null;
        _choice = "";
        _cancelAt = -1;
        _cutAt = -1;
        _closing = false;
        _escaping = false;
        _escapedFrom = null;
        _body.Stop();
    }

    // Out of a trap, before anything else; a chain carries on after it.
    private void Escape()
    {
        GD.Print("Bot: stuck; escaping");

        if (_aside != null)
        {
            _aside.Cancel(_body, "stuck");
            _aside = null;
            _judgeOfAside = null;
        }

        _escapedFrom = _after ?? _activity;

        if (_activity != null)
        {
            _activity.Cancel(_body, "stuck");
            GD.Print("Bot: gave up on \"" + _activity.Name + "\": judged stuck");
        }

        _after = null;
        _closing = false;
        _judge.Forget();
        _escaping = true;
        _activity = BotCatalog.Escape;
        _judgeOfActivity = null;
        _activity.Begin(_body);
    }

    // ---------------------------------------------------------------- the rest

    private string Doing()
    {
        return _persona.Name + ": " + (_chain != null ? _chain.Name + " > " : "")
            + (_aside != null ? _aside.Name + " (aside), during " : "")
            + (_activity?.Name ?? (_closing ? "closing up" : "choosing"));
    }

    private void Finding(string kind, string detail)
    {
        Player? me = _body.Me;

        if (me == null)
        {
            return;
        }

        List<string> open = new List<string>();

        foreach (Control panel in _body.OpenPanels())
        {
            open.Add(panel.GetType().Name);
        }

        GD.Print("Bot: finding " + kind + ": " + detail);
        BotFindings.Write(me, Profile, kind, detail, new Dictionary<string, object?>
        {
            { "zone", _body.ZoneId },
            { "body_age", Math.Round(_body.BodyAge, 1) },
            { "activity", Doing() },
            { "chain", _chain?.Describe() },
            { "step", _activity?.Step?.Name },
            { "online", _body.IsOnline },
            { "open", open },
        });
    }

    // Said in public chat, so whoever watches sees what each bot is after.
    private void Announce(string line)
    {
        _body.Chat("[bot] " + line);
    }

    private bool Allowed(string name)
    {
        return Only.Length == 0 || Array.IndexOf(Only.Split(','), name) >= 0;
    }

    // Ten seconds of Esc, Close, Back and Go Offline, and something is still open: a
    // screen with no way out is a player stuck in it.
    private void CouldNotClose()
    {
        List<string> open = new List<string>();

        foreach (Control panel in _body.OpenPanels())
        {
            open.Add(panel.GetType().Name);
        }

        if (_body.IsOnline)
        {
            open.Add("TerminalScreen (online)");
        }

        string what = string.Join(", ", open);
        GD.Print("Bot: could not close " + what);

        if (open.Count == 0 || (_cannotCloseAt.ContainsKey(what) && _clock - _cannotCloseAt[what] < CannotCloseRepeat))
        {
            return;
        }

        _cannotCloseAt[what] = _clock;
        Finding("cannot-close", "still open after " + (int)_closer.Limit + " s of closing: " + what);
    }

    private void AcceptInvites(double delta)
    {
        Button? join = _body.Usable(InvitePrompt.JoinGroup);

        if (join == null)
        {
            _joinSeenFor = 0;
            _joinDecided = false;
            return;
        }

        if (!_joinDecided)
        {
            // On its own business (a solo chain), never; otherwise now and then.
            _joinDecided = true;
            _willJoin = (_chain == null || !_chain.Solo) && _random.NextDouble() < _persona.JoinChance;
        }

        _joinSeenFor += delta;

        if (_joinSeenFor >= JoinAfter)
        {
            _joinSeenFor = 0;
            Button? no = _body.Usable(InvitePrompt.NoGroup);
            Button? answer = _willJoin || no == null ? join : no;
            GD.Print("Bot: clicking " + answer.Text);
            _body.Click(answer);
        }
    }

    // A real press and release at a screen point, through the same input queue a mouse
    // feeds, so the GUI and the picker both see it. The pointer moves there first, as a
    // hand does: a control learns it is under the pointer from the motion.
    public static void Click(Vector2 at)
    {
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        InputEventMouseButton press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at, GlobalPosition = at };
        InputEventMouseButton release = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = at, GlobalPosition = at };
        Input.ParseInputEvent(press);
        Input.ParseInputEvent(release);
    }

    // Key by key into whatever has the focus, then Enter.
    public static void Type(string text)
    {
        // By code point, as a keyboard sends them: an emoji is two chars but one key.
        foreach (System.Text.Rune rune in text.EnumerateRunes())
        {
            Key key = rune.IsBmp ? KeyFor((char)rune.Value) : Key.None;
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = rune.Value, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Unicode = rune.Value, Pressed = false });
        }

        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
    }

    private static Key KeyFor(char c)
    {
        if (char.IsDigit(c))
        {
            return Key.Key0 + (c - '0');
        }

        if (char.IsLetter(c))
        {
            return Key.A + (char.ToUpperInvariant(c) - 'A');
        }

        switch (c)
        {
            case ' ':
                return Key.Space;
            case '/':
                return Key.Slash;
            case '.':
                return Key.Period;
            case ',':
                return Key.Comma;
            case '?':
                return Key.Question;
            case '!':
                return Key.Exclam;
            case '-':
                return Key.Minus;
            default:
                return Key.Unknown;
        }
    }

    // The first code, in order, that would have given every answer seen so far.
    public static string NextGuess(string[] guesses, int[] exact, int[] partial)
    {
        int count = 1;

        for (int i = 0; i < CodeCracker.Length; i++)
        {
            count *= CodeCracker.Digits;
        }

        for (int n = 0; n < count; n++)
        {
            char[] digits = new char[CodeCracker.Length];
            int rest = n;

            for (int i = CodeCracker.Length - 1; i >= 0; i--)
            {
                digits[i] = (char)('0' + (rest % CodeCracker.Digits));
                rest /= CodeCracker.Digits;
            }

            string candidate = new string(digits);
            bool fits = true;

            for (int g = 0; g < guesses.Length && fits; g++)
            {
                CodeCracker check = new CodeCracker(candidate);
                check.Guess(guesses[g]);
                fits = check.Exact[0] == exact[g] && check.Partial[0] == partial[g];
            }

            if (fits)
            {
                return candidate;
            }
        }

        return "0000";
    }
}
