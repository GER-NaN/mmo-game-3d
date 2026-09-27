namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using Godot;
using MmoGame3d.Gardening;
using MmoGame3d.Players;
using MmoGame3d.Rules.Terminals;
using MmoGame3d.Rules.World;
using MmoGame3d.Town;
using MmoGame3d.Ui;

/// <summary>
/// Every activity a bot can pick. Add one here: a name, a weight, where it can start,
/// and its steps. Each ends back where a next one can start, usually in Old Town.
/// </summary>
public static class BotActivities
{
    private static readonly string[] Lines =
    {
        "anyone seen a battery?",
        "this town needs more lights",
        "hello",
        "found some RAM over here",
        "the drones are out again",
        "lfg substation repair",
    };

    private static readonly string[] Emotes = { "/wave", "/cheer", "/sit", "/pushups" };

    public static readonly BotActivity LeaveParty = new StepsActivity("leave the party", 2, body => body.Usable(PartyPanel.LeaveGroup) != null, body => new List<BotStep>
    {
        new DoStep("click Leave party", 2, (b, d) =>
        {
            Button? leave = b.Usable(PartyPanel.LeaveGroup);

            if (leave != null)
            {
                GD.Print("Bot: clicking Leave party");
                b.Click(leave);
            }

            return StepResult.Done;
        }),
    });

    // Walks back to Old Town from wherever the bot is (a door toward town).
    public static readonly BotActivity GoBackToTown = new StepsActivity("go back to town", 0, OutOfTown, body => new List<BotStep>
    {
        new DoorStep(body.ZoneId == ZoneIds.Greenhouse ? "ToOutskirts" : "ToTown", 150),
    });

    // Not picked by weight: the brain runs it after walks fail twice running.
    public static readonly BotActivity Escape = new StepsActivity("get unstuck", 0, body => true, body => new List<BotStep>
    {
        new EscapeStep(),
    });

    public static readonly BotActivity[] All =
    {
        new StepsActivity("walk around town", 6, ZoneIds.Town, body => new List<BotStep>
        {
            new WanderStep(15 + (body.Random.NextDouble() * 30)),
            Talk(body),
            new WanderStep(10 + (body.Random.NextDouble() * 20)),
            new DoStep("press R for the EMP", 1, (b, d) => { BotBody.Press("emp"); return StepResult.Done; }),
        }),

        new StepsActivity("visit the college", 3, ZoneIds.Town, body => new List<BotStep>
        {
            new DoorStep("ToCollege"),
            new WanderStep(4 + (body.Random.NextDouble() * 4)),
            new WalkToStep("walk to the registrar", b => b.Thing("Interactables/Registrar")),
            new UseStep("Talk to", b => b.IsOpen<CollegePanel>()),
            CollegeWork(),
            new CloseAllStep(),
            Emote(body),
            new WalkToStep("walk to the professor", b => b.Thing("Interactables/Professor")),
            new UseStep("Talk to", b => b.IsOpen<CollegePanel>()),
            CollegeWork(),
            new CloseAllStep(),
            Talk(body),
            new DoorStep("ToTown"),
        }),

        new StepsActivity("go shopping", 3, ZoneIds.Town, body => new List<BotStep>
        {
            new DoorStep("ToShop"),
            new WalkToStep("walk to the shopkeeper", b => b.Thing("Interactables/Shopkeeper")),
            new UseStep("Talk to", b => b.Usable(ShopPanel.BuyGroup) != null),
            new PauseStep(1),
            ClickOne("buy something", ShopPanel.BuyGroup),
            new PauseStep(1),
            new CloseAllStep(),
            new WalkToStep("walk to the workbench", b => b.Thing("Interactables/Workbench")),
            new UseStep("workbench", b => b.IsOpen<WorkbenchPanel>()),
            new PauseStep(0.8),
            ClickOne("take the battery out", WorkbenchPanel.RemoveGroup, true),
            new PauseStep(0.8),
            ClickOne("put a battery in", WorkbenchPanel.InsertGroup, true),
            new PauseStep(0.8),
            new CloseAllStep(),
            new DoorStep("ToTown"),
        }),

        new StepsActivity("use a public terminal", 4, ZoneIds.Town, body =>
        {
            // Chosen once, so it does not swing between the two on the way.
            string terminal = body.Random.Next(2) == 0 ? "Interactables/LibraryTerminal" : "Interactables/StreetKiosk";
            return new List<BotStep>
            {
                new WalkToStep("walk to a terminal", b => b.Thing(terminal)),
                new UseStep("Go Online", b => b.IsOnline),
                new OnlineStep(),
            };
        }),

        new StepsActivity("use the phone", 3, InWorld, body => new List<BotStep>
        {
            PhoneOut(),
            new OnlineStep(),
        }),

        new StepsActivity("check the bag", 2, InWorld, body => new List<BotStep>
        {
            new DoStep("open the bag", 2, (b, d) => { BotBody.Press("inventory"); return StepResult.Done; }),
            new PauseStep(1),
            ClickOne("equip the phone", InventoryPanel.EquipGroup, true),
            new PauseStep(1.5),
            new CloseAllStep(),
        }),

        new StepsActivity("ride a robo taxi", 1, ZoneIds.Town, body => new List<BotStep>
        {
            new WalkToStep("walk to the taxi stand", b => b.Thing("Interactables/TaxiStand")),
            new UseStep("robo taxi", b => b.ZoneId.StartsWith("taxi"), 10, true),
            new DoStep("ride to the drop-off", 180, (b, d) => b.ZoneId == ZoneIds.Town ? StepResult.Done : StepResult.Running, true),
        }),

        new StepsActivity("fix something", 4, HasBrokenThing, body => new List<BotStep>
        {
            new WalkToStep("walk to the broken thing", NearestBroken),
            new UseStep("Fix the", b => NearestBroken(b) == null || !Near(b, NearestBroken(b)!), 10),
        }),

        new StepsActivity("repair the street lights", 1, ZoneIds.Town, body => new List<BotStep>
        {
            new WalkToStep("walk to the junction box", b => b.Thing("Interactables/JunctionBox")),
            new UseStep("Repair", b => !b.Prompt.Contains("Repair"), 5),
        }),

        new StepsActivity("tag the subway", 2, ZoneIds.Town, body => new List<BotStep>
        {
            new DoorStep("ToSubway"),
            new WalkToStep("walk to the wall", b => b.Thing("Interactables/SubwayWall")),
            // Once: a bot that has sprayed before is refused, and the prompt stays.
            new UseStep("Spray", b => false, 4, false, true),
            new WalkToStep("walk to the visitor book", b => b.Thing("Interactables/VisitorBook")),
            new UseStep("visitor book", b => b.IsOpen<VisitorBookPanel>()),
            new PauseStep(1.5),
            ClickOne("turn a page", VisitorBookPanel.NextGroup, true),
            new PauseStep(1.5),
            new CloseAllStep(),
            new DoorStep("ToTown"),
        }),

        new StepsActivity("go to the outskirts", 2, ZoneIds.Town, body => new List<BotStep>
        {
            new DoorStep("ToOutskirts"),
            new WanderStep(6 + (body.Random.NextDouble() * 8)),
            new WalkToStep("walk to the chest", b => b.Thing("Interactables/OldHardwareChest")),
            new UseStep("Open the", b => !b.Prompt.Contains("Open the"), 4),
            Emote(body),
            new DoorStep("ToTown"),
        }),

        new StepsActivity("walk the meadows", 2, ZoneIds.Town, body => new List<BotStep>
        {
            new DoorStep("ToMeadows"),
            new WanderStep(20 + (body.Random.NextDouble() * 25)),
            new DoorStep("ToTown", 150),
        }),

        new StepsActivity("meet someone", 4, InWorld, body => new List<BotStep>
        {
            new MeetStep(),
            new PauseStep(1),
            new CloseAllStep(),
        }),

        LeaveParty,

        new StepsActivity("recycle something", 1, ZoneIds.Town, body => new List<BotStep>
        {
            new WalkToStep("walk to the recycler", b => b.Thing("Interactables/Recycler")),
            new UseStep("recycler", b => b.IsOpen<RecyclerPanel>()),
            new PauseStep(1),
            ClickOne("recycle", RecyclerPanel.RecycleGroup, true),
            new PauseStep(1),
            new CloseAllStep(),
        }),

        new StepsActivity("look at the map", 1, InWorld, body => new List<BotStep>
        {
            new DoStep("open the map", 1, (b, d) => { BotBody.Press("map"); return StepResult.Done; }),
            new PauseStep(3),
            new CloseAllStep(),
        }),

        new StepsActivity("look at skills and friends", 1, InWorld, body => new List<BotStep>
        {
            new DoStep("open skills", 1, (b, d) => { BotBody.Press("skills"); return StepResult.Done; }),
            new PauseStep(2.5),
            new CloseAllStep(),
            new DoStep("open friends", 1, (b, d) => { BotBody.Press("social"); return StepResult.Done; }),
            new PauseStep(2.5),
            new CloseAllStep(),
        }),

        // Somewhere other than town and not on a ride (pulled through a door by the party,
        // or a plan that failed half way): walk back.
        new StepsActivity("go back to town", 20, OutOfTown, body => new List<BotStep>
        {
            new DoorStep(body.ZoneId == ZoneIds.Greenhouse ? "ToOutskirts" : "ToTown", 150),
        }),
    };

    private static bool InWorld(BotBody body)
    {
        return body.Zone != null && !body.ZoneId.StartsWith("taxi");
    }

    private static bool InTown(BotBody body)
    {
        return body.ZoneId == ZoneIds.Town;
    }

    private static bool OutOfTown(BotBody body)
    {
        return InWorld(body) && body.ZoneId != ZoneIds.Town;
    }

    private static bool HasBrokenThing(BotBody body)
    {
        return InWorld(body) && NearestBroken(body) != null;
    }

    private static Node3D? NearestBroken(BotBody body)
    {
        Node? things = body.Zone?.GetNodeOrNull("Interactables");
        Fixable? nearest = null;

        if (things == null)
        {
            return null;
        }

        foreach (Node node in things.GetChildren())
        {
            Fixable? fixable = node as Fixable;

            if (fixable != null && fixable.Broken && (nearest == null || body.DistanceTo(fixable.GlobalPosition) < body.DistanceTo(nearest.GlobalPosition)))
            {
                nearest = fixable;
            }
        }

        return nearest;
    }

    private static bool Near(BotBody body, Node3D thing)
    {
        return body.DistanceTo(thing.GlobalPosition) < 3f;
    }

    private static BotStep Talk(BotBody body)
    {
        string line = Lines[body.Random.Next(Lines.Length)];
        return new DoStep("say \"" + line + "\"", 1, (b, d) => { b.Chat(line); return StepResult.Done; });
    }

    private static BotStep Emote(BotBody body)
    {
        string emote = Emotes[body.Random.Next(Emotes.Length)];
        return new DoStep("emote " + emote, 1, (b, d) => { b.Chat(emote); return StepResult.Done; });
    }

    // P once, then online within a few seconds, or the phone is dead or not equipped.
    private static BotStep PhoneOut()
    {
        bool pressed = false;
        return new DoStep("take the phone out", 4, (b, d) =>
        {
            if (b.IsOnline)
            {
                return StepResult.Done;
            }

            if (!pressed)
            {
                pressed = true;
                b.Stop();
                BotBody.Press("phone");
            }

            return StepResult.Running;
        });
    }

    // Clicks one of the group's buttons, a random one. Optional: done with none there.
    private static BotStep ClickOne(string name, string group, bool optional = false)
    {
        // Picked once, so scrolling one into view does not change the mind.
        Button? pick = null;
        return new DoStep(name, 4, (b, d) =>
        {
            if (pick == null || !GodotObject.IsInstanceValid(pick) || !pick.IsVisibleInTree())
            {
                List<Button> buttons = b.UsableAll(group);

                if (buttons.Count == 0)
                {
                    return optional ? StepResult.Done : StepResult.Running;
                }

                pick = buttons[b.Random.Next(buttons.Count)];
            }

            if (!b.TryClick(pick))
            {
                return StepResult.Running;
            }

            GD.Print("Bot: clicked " + pick.Text);
            return StepResult.Done;
        });
    }

    // At the registrar or the professor: the Class, a career, a rank, whichever is there.
    private static BotStep CollegeWork()
    {
        double read = 0;
        return new DoStep("work the college panel", 6, (b, d) =>
        {
            read += d;

            if (read < 1)
            {
                return StepResult.Running;
            }

            Button? button = b.Usable(CollegePanel.ClassGroup) ?? b.Usable(CollegePanel.EnrollGroup) ?? b.Usable(CollegePanel.RankUpGroup);

            if (button != null)
            {
                GD.Print("Bot: clicking " + button.Text);
                b.Click(button);
            }

            read = 0;
            return StepResult.Done;
        });
    }
}


