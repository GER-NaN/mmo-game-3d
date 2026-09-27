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

    public static readonly BotActivity PickUpItems = new StepsActivity("pick up things", 0, InWorld, body => new List<BotStep>
    {
        new PickUpStep(),
    });

    public static readonly BotActivity Recycle = new StepsActivity("recycle for money", 0, ZoneIds.Town, body => new List<BotStep>
    {
        new WalkToStep("walk to the recycler", b => b.Thing("Interactables/Recycler")),
        new UseStep("recycler", b => b.IsOpen<RecyclerPanel>()),
        new PauseStep(1),
        Click("recycle something", RecyclerPanel.RecycleGroup, ""),
        new PauseStep(1),
        new CloseAllStep(),
    });

    public static readonly BotActivity ReportDrones = new StepsActivity("report drones on the cameras", 0, ZoneIds.Town, body => new List<BotStep>
    {
        new WalkToStep("walk to the library terminal", b => b.Thing("Interactables/LibraryTerminal")),
        new UseStep("Go Online", b => b.IsOnline),
        Click("open Town cameras", TerminalScreen.AppGroupPrefix + TerminalApps.TownCameras, ""),
        new CameraStep(),
        new CloseAllStep(),
    });

    // On either public terminal, or on the phone when it has one with charge: every way
    // in to the game gets played.
    public static readonly BotActivity PlayDefense = new StepsActivity("play Agent Defense", 0, ZoneIds.Town, body =>
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

    public static readonly BotActivity HuntDrones = new StepsActivity("hunt a drone", 0, ZoneIds.Town, body => new List<BotStep>
    {
        new HuntStep(),
    });

    public static readonly BotActivity Explore = new StepsActivity("explore", 0, InWorld, body => new List<BotStep>
    {
        new ExploreStep(),
    });

    public static readonly BotActivity SwapBattery = new StepsActivity("swap the battery", 0, ZoneIds.Town, body => new List<BotStep>
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

    public static readonly BotActivity DropSomething = new StepsActivity("drop something", 0, InWorld, body => new List<BotStep>
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

        new BotGoal("explore this zone", 2, body => InWorld(body) && body.Undiscovered().Count > 0, (body, state) =>
        {
            return body.Undiscovered().Count > 0 ? Explore : null;
        }, 8, 300),

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
        return new StepsActivity("buy " + name, 0, ZoneIds.Town, body => new List<BotStep>
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
        return new StepsActivity("equip " + name, 0, InWorld, body => new List<BotStep>
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

            if (!b.TryClick(button))
            {
                return StepResult.Running;
            }

            GD.Print("Bot: clicked " + button.Text + (itemName.Length > 0 ? " (" + itemName + ")" : ""));
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





