namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.World;
using MmoGame3d.Ui;

/// <summary>
/// Something a bot wants, reached through activities it chooses by what it has: to fight
/// drones it needs an EMP worn; without one in the bag it buys one; without the money it
/// earns it (picks things up and recycles them, or reports drones on the cameras); then
/// it hunts. After each activity, Next looks at the bot again and names the next one,
/// or null when the goal is met. Budget caps the activities it may take.
///
/// A solo goal is the bot's own business: it leaves its party first and turns invites
/// down until the goal ends, so bots after different things do not drag each other
/// through doors. Parties belong to wander mode, when a bot has no goal.
/// </summary>
public sealed class BotGoal
{
    public BotGoal(string name, int weight, Func<BotBody, bool> canStart, Func<BotBody, GoalState, BotActivity?> next, int budget, double usualSeconds, bool solo = true)
    {
        Name = name;
        Weight = weight;
        CanStart = canStart;
        Next = next;
        Budget = budget;
        UsualSeconds = usualSeconds;
        Solo = solo;
    }

    public bool Solo { get; }

    public string Name { get; }

    public int Weight { get; }

    public Func<BotBody, bool> CanStart { get; }

    public Func<BotBody, GoalState, BotActivity?> Next { get; }

    public int Budget { get; }

    // About how long it takes: a random drop falls somewhere inside it.
    public double UsualSeconds { get; }
}

/// <summary>What a goal has seen so far: its activities and how the last one went.</summary>
public sealed class GoalState
{
    public int Rounds { get; set; }

    public string LastActivity { get; set; } = "";

    public bool LastFinished { get; set; }

    // A count the goal keeps: drones brought down, runs played.
    public int Wins { get; set; }

    // A number the goal aims at: money to reach.
    public int Target { get; set; } = -1;

    // Set with a null Next when the goal cannot be met now: why it gave up.
    public string GiveUp { get; set; } = "";
}

/// <summary>Every goal, and the activities only goals start.</summary>
public static class BotGoals
{
    private const int EmpPrice = 10;
    private const int BatteryPrice = 8;

    public static readonly BotActivity PickUpItems = new BotActivity("pick up things", 0, InWorld, body => new List<BotStep>
    {
        new PickUpStep(),
    });

    public static readonly BotActivity Recycle = new BotActivity("recycle for money", 0, InTown, body => new List<BotStep>
    {
        new WalkToStep("walk to the recycler", b => b.Thing("Interactables/Recycler")),
        new UseStep("recycler", b => b.IsOpen<RecyclerPanel>()),
        new PauseStep(1),
        Click("recycle something", RecyclerPanel.RecycleGroup, ""),
        new PauseStep(1),
        new CloseAllStep(),
    });

    public static readonly BotActivity ReportDrones = new BotActivity("report drones on the cameras", 0, InTown, body => new List<BotStep>
    {
        new WalkToStep("walk to the library terminal", b => b.Thing("Interactables/LibraryTerminal")),
        new UseStep("Go Online", b => b.IsOnline),
        Click("open Town cameras", TerminalScreen.AppGroupPrefix + TerminalApps.TownCameras, ""),
        new CameraStep(),
        new CloseAllStep(),
    });

    // On either public terminal, or on the phone when it has one with charge: every way
    // in to the game gets played.
    public static readonly BotActivity PlayDefense = new BotActivity("play Agent Defense", 0, InTown, body =>
    {
        bool phone = body.Has(ItemType.Phone) && body.PhonePercent > 20;
        int where = body.Random.Next(phone ? 3 : 2);
        List<BotStep> steps;

        if (where == 2)
        {
            steps = new BotPlan().Equip(ItemType.Phone).Phone().Steps;
        }
        else
        {
            string terminal = where == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
            steps = new List<BotStep>
            {
                new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
                new UseStep("Go Online", b => b.IsOnline),
            };
        }

        steps.Add(Click("open Defense Objectives", TerminalScreen.AppGroupPrefix + TerminalApps.Defense, ""));
        steps.Add(new PauseStep(0.8));
        steps.Add(Click("start a run", TerminalScreen.DefenseStartGroup, ""));
        steps.Add(new DefenseStep());
        steps.Add(new CloseAllStep());
        return steps;
    });

    public static readonly BotActivity HuntDrones = new BotActivity("hunt a drone", 0, InTown, body => new List<BotStep>
    {
        new HuntStep(),
    });

    public static readonly BotActivity Explore = new BotActivity("explore", 0, InWorld, body => new List<BotStep>
    {
        new ExploreStep(),
    });

    public static readonly BotActivity SwapBattery = new BotActivity("swap the battery", 0, InTown, body => new List<BotStep>
    {
        new DoorStep("ToShop"),
        new WalkToStep("walk to the workbench", b => b.Thing("Interactables/Workbench")),
        new UseStep("workbench", b => b.IsOpen<WorkbenchPanel>()),
        new PauseStep(0.8),
        Click("take the dead battery out", WorkbenchPanel.RemoveGroup, "", true),
        new PauseStep(0.8),
        Click("put a full one in", WorkbenchPanel.InsertGroup, "", true),
        new PauseStep(0.8),
        new CloseAllStep(),
        new DoorStep("ToTown"),
    });

    public static readonly BotActivity DropSomething = new BotActivity("drop something", 0, InWorld, body => new List<BotStep>
    {
        new DoStep("open the bag", 1, (b, d) => { BotBody.Press("inventory"); return StepResult.Done; }),
        new PauseStep(1),
        Click("drop a stack", InventoryPanel.DropGroup, "", true),
        new PauseStep(1),
        new CloseAllStep(),
    });

    public static readonly BotGoal[] All =
    {
        new BotGoal("fight drones", 4, InTown, (body, state) =>
        {
            if (state.LastActivity == HuntDrones.Name && state.LastFinished)
            {
                state.Wins++;
            }

            if (state.Wins >= 2)
            {
                return null;
            }

            if (!body.Wears(ItemType.EmpEmitter))
            {
                return body.Has(ItemType.EmpEmitter) ? Equip(ItemType.EmpEmitter) : body.Money >= EmpPrice ? Buy(ItemType.EmpEmitter) : Earn(body);
            }

            if (body.LiveDrones().Count == 0)
            {
                state.GiveUp = "no drones flying";
                return null;
            }

            return HuntDrones;
        }, 12, 300),

        new BotGoal("charge the phone", 5, body => InTown(body) && body.PhonePercent >= 0 && body.PhonePercent < 20, (body, state) =>
        {
            if (body.PhonePercent >= 50)
            {
                return null;
            }

            // A swap that finished and left the phone low worked on another phone, or put
            // in a battery no better: again would only loop.
            if (state.LastActivity == SwapBattery.Name && state.LastFinished)
            {
                state.GiveUp = "the swap left the phone at " + body.PhonePercent + "%";
                return null;
            }

            // Only a battery clearly fuller than the phone's is worth the trip; the old one
            // comes back into the bag after a swap.
            bool better = body.SpareBatteryPercent >= Math.Max(50, body.PhonePercent + 30);
            return better ? SwapBattery : body.Money >= BatteryPrice ? Buy(ItemType.Battery) : Earn(body);
        }, 10, 240),

        new BotGoal("explore this zone", 2, body => InWorld(body) && body.Undiscovered().Count > 0, (body, state) =>
        {
            return body.Undiscovered().Count > 0 ? Explore : null;
        }, 8, 300),

        new BotGoal("earn some money", 2, InTown, (body, state) =>
        {
            if (state.Target < 0)
            {
                state.Target = body.Money + 10;
            }

            return body.Money >= state.Target ? null : Earn(body);
        }, 8, 240),

        new BotGoal("play Agent Defense", 3, InTown, (body, state) =>
        {
            return state.LastActivity == PlayDefense.Name && state.LastFinished ? null : PlayDefense;
        }, 3, 120),

        new BotGoal("tidy the bag", 1, body => InWorld(body) && body.HasSomethingToSell, (body, state) =>
        {
            return state.LastActivity == DropSomething.Name ? null : DropSomething;
        }, 2, 30),
    };

    // Money without spending any: sell what is carried, pick up what lies about, or
    // report drones for the town's pay.
    private static BotActivity Earn(BotBody body)
    {
        if (body.HasSomethingToSell)
        {
            return Recycle;
        }

        return body.GroundItems().Count > 0 ? PickUpItems : ReportDrones;
    }

    private static BotActivity Buy(ItemType type)
    {
        string name = ItemCatalog.Get(type).Name;
        return new BotActivity("buy " + name, 0, InTown, body => new List<BotStep>
        {
            new DoorStep("ToShop"),
            new WalkToStep("walk to the shopkeeper", b => b.Thing("Interactables/Shopkeeper")),
            new UseStep("Talk to", b => b.Usable(ShopPanel.BuyGroup) != null),
            new PauseStep(1),
            Click("buy " + name, ShopPanel.BuyGroup, name),
            new PauseStep(1.5),
            new CloseAllStep(),
            new DoorStep("ToTown"),
        });
    }

    private static BotActivity Equip(ItemType type)
    {
        string name = ItemCatalog.Get(type).Name;
        return new BotActivity("equip " + name, 0, InWorld, body => new List<BotStep>
        {
            new DoStep("open the bag", 1, (b, d) => { BotBody.Press("inventory"); return StepResult.Done; }),
            new PauseStep(1),
            Click("equip " + name, InventoryPanel.EquipGroup, name),
            new PauseStep(1),
            new CloseAllStep(),
        });
    }

    // One of the group's buttons: on the row naming the item, or any with "".
    private static BotStep Click(string name, string group, string itemName, bool optional = false)
    {
        return new DoStep(name, 4, (b, d) =>
        {
            Button? button = itemName.Length > 0 ? b.RowButton(group, itemName) : b.Usable(group);

            if (button == null)
            {
                return optional ? StepResult.Done : StepResult.Running;
            }

            GD.Print("Bot: clicking " + button.Text + (itemName.Length > 0 ? " (" + itemName + ")" : ""));
            b.Click(button);
            return StepResult.Done;
        });
    }

    private static bool InWorld(BotBody body)
    {
        return body.Zone != null && !body.ZoneId.StartsWith("taxi");
    }

    private static bool InTown(BotBody body)
    {
        return body.ZoneId == ZoneIds.Town;
    }
}

/// <summary>Walks over the things lying about (walking over one picks it up), up to three.</summary>
public sealed class PickUpStep : BotStep
{
    private Walker _walker = new Walker();
    private Node3D? _item;
    private int _taken;

    public PickUpStep()
        : base("pick things up", 90)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _item != null && GodotObject.IsInstanceValid(_item) && _item.IsInsideTree() ? _item.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _item = null;
        _taken = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_item == null || !GodotObject.IsInstanceValid(_item) || !_item.IsInsideTree())
        {
            if (_item != null)
            {
                _taken++;
            }

            _item = body.Nearest(body.GroundItems());
            _walker = new Walker();

            if (_item == null || _taken >= 3)
            {
                body.Stop();
                return _taken > 0 ? StepResult.Done : StepResult.Failed;
            }
        }

        // Near is below zero: it walks onto the thing until it is picked up and gone.
        if (_walker.Walk(body, _item.GlobalPosition, -1f, delta) == StepResult.Failed)
        {
            body.Unreachable.Add(_item.Name);
            return StepResult.Failed;
        }

        return StepResult.Running;
    }
}

/// <summary>Walks under the nearest drone and fires the EMP until it is down.</summary>
public sealed class HuntStep : BotStep
{
    private const float Under = 5f;
    private const double FireEvery = 1.5;

    private Walker _walker = new Walker();
    private Node3D? _drone;
    private double _fireIn;

    public HuntStep()
        : base("hunt a drone", 60)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _drone != null && GodotObject.IsInstanceValid(_drone) && _drone.IsInsideTree() ? _drone.GlobalPosition : null;
    }

    public override void Begin(BotBody body)
    {
        _drone = body.Nearest(body.LiveDrones());
        _walker = new Walker();
        _fireIn = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_drone == null)
        {
            return StepResult.Failed;
        }

        if (!GodotObject.IsInstanceValid(_drone) || !_drone.IsInsideTree() || ((Drones.Drone)_drone).Down)
        {
            body.Stop();
            GD.Print("Bot: the drone is down");
            return StepResult.Done;
        }

        // The ground under it, at the bot's own height: a drone over a building puts the
        // nearest point of the air on the roof, and the walk presses into the wall.
        Players.Player? me = body.Me;
        Vector3 under = me == null ? _drone.GlobalPosition : new Vector3(_drone.GlobalPosition.X, me.GlobalPosition.Y, _drone.GlobalPosition.Z);
        _walker.Walk(body, under, Under, delta);
        _fireIn -= delta;

        if (_fireIn <= 0 && body.DistanceTo(_drone.GlobalPosition) < Under * 2f)
        {
            _fireIn = FireEvery;
            BotBody.Press("emp");
        }

        return StepResult.Running;
    }
}

/// <summary>
/// Plays an Agent Defense run: each cue's lane key as the cue crosses the line, until the
/// run is over.
/// </summary>
public sealed class DefenseStep : BotStep
{
    private int _pressed = -1;
    private bool _started;

    public DefenseStep()
        : base("play the run", 90)
    {
    }

    public override void Begin(BotBody body)
    {
        _pressed = -1;
        _started = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        AgentDefenseView? view = body.Me?.GetTree().GetFirstNodeInGroup(AgentDefenseView.Group) as AgentDefenseView;

        if (view == null || !view.Playing)
        {
            // Over once it had started: the server scored it.
            return _started ? StepResult.Done : StepResult.Running;
        }

        _started = true;
        int clock = view.Clock;

        foreach (DefenseCue cue in view.Cues)
        {
            if (cue.AtMs > _pressed && cue.AtMs <= clock)
            {
                Key key = AgentDefenseView.LaneKeys[cue.Lane];
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = false });
            }
        }

        _pressed = Math.Max(_pressed, clock);
        return StepResult.Running;
    }
}

/// <summary>
/// Watches the Town cameras until a drone is in the picture and clicks it, which
/// reports it for the town's pay. The cameras change by themselves every few seconds.
/// </summary>
public sealed class CameraStep : BotStep
{
    private double _waited;

    public CameraStep()
        : base("report a drone", 45)
    {
    }

    public override void Begin(BotBody body)
    {
        _waited = 0;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        _waited += delta;
        CctvView? view = body.Me?.GetTree().GetFirstNodeInGroup(CctvView.Group) as CctvView;

        if (view == null || _waited < 1)
        {
            return StepResult.Running;
        }

        Vector2? at = view.ScreenPointOfADrone();

        if (at == null)
        {
            return StepResult.Running;
        }

        GD.Print("Bot: clicking a drone on camera " + view.CameraNumber);
        BotDriver.Click(at.Value);
        return StepResult.Done;
    }
}

/// <summary>Walks to the parts of the map not discovered yet, nearest first.</summary>
public sealed class ExploreStep : BotStep
{
    private const float Near = 6f;

    private readonly List<Vector3> _unreachable = new List<Vector3>();
    private Walker _walker = new Walker();
    private Vector3? _spot;
    private int _reached;

    public ExploreStep()
        : base("explore", 90)
    {
    }

    public override bool Walks
    {
        get { return true; }
    }

    public override Vector3? Target(BotBody body)
    {
        return _spot;
    }

    public override void Begin(BotBody body)
    {
        _spot = null;
        _reached = 0;
        _unreachable.Clear();
    }

    private bool Unreachable(Vector3 spot)
    {
        foreach (Vector3 failed in _unreachable)
        {
            if (failed.DistanceTo(spot) < Rules.Maps.Discovery.CellSize)
            {
                return true;
            }
        }

        return false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (_spot == null)
        {
            Vector3? best = null;

            foreach (Vector3 spot in body.Undiscovered())
            {
                if (!Unreachable(spot) && (best == null || body.DistanceTo(spot) < body.DistanceTo(best.Value)))
                {
                    best = spot;
                }
            }

            if (best == null)
            {
                return StepResult.Done;
            }

            _spot = best;
            _walker = new Walker();
        }

        StepResult walked = _walker.Walk(body, _spot.Value, Near, delta);

        if (walked == StepResult.Running)
        {
            return StepResult.Running;
        }

        // Out of reach (inside a building, past an edge): not tried again this step.
        if (walked == StepResult.Failed)
        {
            _unreachable.Add(_spot.Value);
        }

        _spot = null;
        _reached++;
        return _reached >= 4 ? StepResult.Done : StepResult.Running;
    }
}
