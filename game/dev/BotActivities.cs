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
/// Something a bot sets out to do, such as visiting the college: a plan of steps made
/// fresh each time it starts. It may start only where it makes sense (CanStart), and it
/// ends when its steps are done or one of them fails; either way the bot closes what is
/// open and picks the next.
/// </summary>
public sealed class BotActivity
{
    public BotActivity(string name, int weight, Func<BotBody, bool> canStart, Func<BotBody, List<BotStep>> plan)
    {
        Name = name;
        Weight = weight;
        CanStart = canStart;
        Plan = plan;
    }

    public string Name { get; }

    // How often it is picked, against the others that can start.
    public int Weight { get; }

    public Func<BotBody, bool> CanStart { get; }

    public Func<BotBody, List<BotStep>> Plan { get; }
}

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

    public static readonly BotActivity LeaveParty = new BotActivity("leave the party", 2, body => body.Usable(PartyPanel.LeaveGroup) != null, body => new List<BotStep>
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
    public static readonly BotActivity GoBackToTown = new BotActivity("go back to town", 0, OutOfTown, body => new List<BotStep>
    {
        new DoorStep(body.ZoneId == ZoneIds.Greenhouse ? "ToOutskirts" : "ToTown", 150),
    });

    // Not picked by weight: the brain runs it after walks fail twice running.
    public static readonly BotActivity Escape = new BotActivity("get unstuck", 0, body => true, body => new List<BotStep>
    {
        new EscapeStep(),
    });

    public static readonly BotActivity[] All =
    {
        new BotActivity("walk around town", 6, InTown, body => new List<BotStep>
        {
            new WanderStep(15 + (body.Random.NextDouble() * 30)),
            Talk(body),
            new WanderStep(10 + (body.Random.NextDouble() * 20)),
            new DoStep("press R for the EMP", 1, (b, d) => { BotBody.Press("emp"); return StepResult.Done; }),
        }),

        new BotActivity("visit the college", 3, InTown, body => new List<BotStep>
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

        new BotActivity("go shopping", 3, InTown, body => new List<BotStep>
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

        new BotActivity("use a public terminal", 4, InTown, body =>
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

        new BotActivity("use the phone", 3, InWorld, body => new List<BotStep>
        {
            PhoneOut(),
            new OnlineStep(),
        }),

        new BotActivity("check the bag", 2, InWorld, body => new List<BotStep>
        {
            new DoStep("open the bag", 2, (b, d) => { BotBody.Press("inventory"); return StepResult.Done; }),
            new PauseStep(1),
            ClickOne("equip the phone", InventoryPanel.EquipGroup, true),
            new PauseStep(1.5),
            new CloseAllStep(),
        }),

        new BotActivity("ride a robo taxi", 1, InTown, body => new List<BotStep>
        {
            new WalkToStep("walk to the taxi stand", b => b.Thing("Interactables/TaxiStand")),
            new UseStep("robo taxi", b => b.ZoneId.StartsWith("taxi"), 10, true),
            new DoStep("ride to the drop-off", 180, (b, d) => b.ZoneId == ZoneIds.Town ? StepResult.Done : StepResult.Running, true),
        }),

        new BotActivity("fix something", 4, HasBrokenThing, body => new List<BotStep>
        {
            new WalkToStep("walk to the broken thing", NearestBroken),
            new UseStep("Fix the", b => NearestBroken(b) == null || !Near(b, NearestBroken(b)!), 10),
        }),

        new BotActivity("repair the street lights", 1, InTown, body => new List<BotStep>
        {
            new WalkToStep("walk to the junction box", b => b.Thing("Interactables/JunctionBox")),
            new UseStep("Repair", b => !b.Prompt.Contains("Repair"), 5),
        }),

        new BotActivity("tag the subway", 2, InTown, body => new List<BotStep>
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

        new BotActivity("go to the outskirts", 2, InTown, body => new List<BotStep>
        {
            new DoorStep("ToOutskirts"),
            new WanderStep(6 + (body.Random.NextDouble() * 8)),
            new WalkToStep("walk to the chest", b => b.Thing("Interactables/OldHardwareChest")),
            new UseStep("Open the", b => !b.Prompt.Contains("Open the"), 4),
            Emote(body),
            new DoorStep("ToTown"),
        }),

        new BotActivity("make a house plant", 1, InTown, body => new List<BotStep>
        {
            new DoorStep("ToOutskirts"),
            new DoorStep("ToGreenhouse"),
            new WalkToStep("walk to the potting table", b => b.Thing("Interactables/PottingTable")),
            new UseStep("house plant", b => b.IsOpen<GardenScreen>()),
            new GardenStep(),
            new CloseAllStep(),
            new DoorStep("ToOutskirts"),
            new DoorStep("ToTown"),
        }),

        new BotActivity("walk the meadows", 2, InTown, body => new List<BotStep>
        {
            new DoorStep("ToMeadows"),
            new WanderStep(20 + (body.Random.NextDouble() * 25)),
            new DoorStep("ToTown", 150),
        }),

        new BotActivity("meet someone", 4, InWorld, body => new List<BotStep>
        {
            new MeetStep(),
            new PauseStep(1),
            new CloseAllStep(),
        }),

        LeaveParty,

        new BotActivity("recycle something", 1, InTown, body => new List<BotStep>
        {
            new WalkToStep("walk to the recycler", b => b.Thing("Interactables/Recycler")),
            new UseStep("recycler", b => b.IsOpen<RecyclerPanel>()),
            new PauseStep(1),
            ClickOne("recycle", RecyclerPanel.RecycleGroup, true),
            new PauseStep(1),
            new CloseAllStep(),
        }),

        new BotActivity("look at the map", 1, InWorld, body => new List<BotStep>
        {
            new DoStep("open the map", 1, (b, d) => { BotBody.Press("map"); return StepResult.Done; }),
            new PauseStep(3),
            new CloseAllStep(),
        }),

        new BotActivity("look at skills and friends", 1, InWorld, body => new List<BotStep>
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
        new BotActivity("go back to town", 20, OutOfTown, body => new List<BotStep>
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
        return new DoStep(name, 3, (b, d) =>
        {
            List<Button> buttons = b.UsableAll(group);

            if (buttons.Count == 0)
            {
                return optional ? StepResult.Done : StepResult.Running;
            }

            Button pick = buttons[b.Random.Next(buttons.Count)];
            GD.Print("Bot: clicking " + pick.Text);
            b.Click(pick);
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

/// <summary>
/// Online at a terminal or on the phone: looks at a few apps, a random one each time,
/// sometimes cracks a code or takes the repair job, then goes offline and checks that it
/// did.
/// </summary>
public sealed class OnlineStep : BotStep
{
    private static readonly string[] BrowsedApps =
    {
        TerminalApps.Chat, TerminalApps.Online, TerminalApps.Whois, TerminalApps.TodoList, TerminalApps.TownLog,
        TerminalApps.TownCameras, TerminalApps.StatusBoard, TerminalApps.ExchangeRate, TerminalApps.Defense,
    };

    private enum Phase
    {
        Pick,
        Read,
        Crack,
        Leave,
    }

    private Phase _phase;
    private int _appsLeft;
    private double _wait;
    private bool _jobClicked;
    private int _crackStep;
    private int _crackSeen;

    public OnlineStep()
        : base("be online", 180)
    {
    }

    public override void Begin(BotBody body)
    {
        _phase = Phase.Pick;
        _appsLeft = 1 + body.Random.Next(3);
        _wait = 0.8;
        _jobClicked = false;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        if (!body.IsOnline)
        {
            return _phase == Phase.Leave ? StepResult.Done : StepResult.Failed;
        }

        _wait -= delta;

        if (_wait > 0)
        {
            return StepResult.Running;
        }

        switch (_phase)
        {
            case Phase.Pick:
                Pick(body);
                break;
            case Phase.Read:
                Button? take = body.Usable(TerminalScreen.TakeJobGroup);

                if (take != null && !_jobClicked)
                {
                    _jobClicked = true;
                    GD.Print("Bot: clicking Take the job");
                    body.Click(take);
                    _wait = 1;
                    break;
                }

                NextApp();
                break;
            case Phase.Crack:
                if (!Crack(body))
                {
                    NextApp();
                }

                break;
            default:
                body.CloseOne();
                _wait = 1.5;
                break;
        }

        return StepResult.Running;
    }

    private void NextApp()
    {
        _appsLeft--;
        _phase = _appsLeft > 0 ? Phase.Pick : Phase.Leave;
        _wait = 0.5;
    }

    private void Pick(BotBody body)
    {
        Button? cracker = body.Usable(TerminalScreen.AppGroupPrefix + TerminalApps.CodeCracker);

        if (cracker != null && body.Random.Next(3) == 0)
        {
            GD.Print("Bot: opening the code cracker");
            body.Click(cracker);
            _phase = Phase.Crack;
            _crackStep = 0;
            _wait = 0.6;
            return;
        }

        List<Button> apps = new List<Button>();

        foreach (string id in BrowsedApps)
        {
            Button? app = body.Usable(TerminalScreen.AppGroupPrefix + id);

            if (app != null)
            {
                apps.Add(app);
            }
        }

        if (apps.Count == 0)
        {
            _phase = Phase.Leave;
            return;
        }

        Button pick = apps[body.Random.Next(apps.Count)];
        GD.Print("Bot: opening " + pick.Text);
        body.Click(pick);
        _phase = Phase.Read;
        _wait = 2 + (body.Random.NextDouble() * 4);
    }

    // Start a code, then a guess each time a new answer shows, until cracked or locked.
    // False when finished.
    private bool Crack(BotBody body)
    {
        TerminalScreen? screen = body.Me?.GetTree().GetFirstNodeInGroup(TerminalScreen.GoOfflineGroup)?.Owner as TerminalScreen;
        _wait = 0.4;

        if (screen == null)
        {
            return false;
        }

        if (_crackStep == 0)
        {
            Button? start = body.Usable(TerminalScreen.CrackStartGroup);

            if (start == null)
            {
                return false;
            }

            body.Click(start);
            _crackSeen = -1;
            _crackStep = 1;
            return true;
        }

        string[]? guesses = screen.CrackGuesses;

        if (guesses == null || guesses.Length == _crackSeen)
        {
            return true;
        }

        if (screen.CrackStatus != 0)
        {
            GD.Print("Bot: code " + (screen.CrackStatus == 1 ? "cracked" : "locked out") + " in " + guesses.Length + " guesses");
            return false;
        }

        _crackSeen = guesses.Length;
        BotDriver.Type(BotDriver.NextGuess(guesses, screen.CrackExact, screen.CrackPartial));
        return true;
    }
}

/// <summary>
/// At the potting table: drags three pieces onto the soil, then Complete, a name and
/// Finish. Each drag is a press on a piece, a move over the soil, a release there.
/// </summary>
public sealed class GardenStep : BotStep
{
    private static readonly Vector2[] Spots = { new Vector2(0f, 0f), new Vector2(0.45f, 0.2f), new Vector2(-0.35f, -0.3f) };

    private int _step;
    private double _wait;

    public GardenStep()
        : base("make a plant", 40)
    {
    }

    public override void Begin(BotBody body)
    {
        _step = 0;
        _wait = 0.8;
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        GardenScreen? screen = null;

        foreach (Node node in body.Me!.GetTree().GetNodesInGroup(GardenScreen.CompleteGroup))
        {
            screen = node.FindParent("GardenScreen") as GardenScreen;
        }

        if (screen == null)
        {
            return StepResult.Failed;
        }

        _wait -= delta;

        if (_wait > 0)
        {
            return StepResult.Running;
        }

        _wait = 0.4;
        int piece = _step / 3;

        if (piece < Spots.Length)
        {
            Vector2 spot = screen.ScreenPointOnSoil(Spots[piece].X, Spots[piece].Y);

            switch (_step % 3)
            {
                case 0:
                    Godot.Collections.Array<Node> pieces = body.Me!.GetTree().GetNodesInGroup(GardenScreen.PieceGroup);
                    Button button = (Button)pieces[(piece * 4) % pieces.Count];
                    Vector2 at = button.GetGlobalRect().GetCenter();
                    Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = at, GlobalPosition = at });
                    break;
                case 1:
                    Input.ParseInputEvent(new InputEventMouseMotion { Position = spot, GlobalPosition = spot, Relative = Vector2.One });
                    break;
                default:
                    Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = spot, GlobalPosition = spot });
                    break;
            }

            _step++;
            return StepResult.Running;
        }

        switch (_step - (Spots.Length * 3))
        {
            case 0:
                Button? complete = body.Usable(GardenScreen.CompleteGroup);

                if (complete != null)
                {
                    GD.Print("Bot: clicking Complete");
                    body.Click(complete);
                }

                break;
            case 1:
                BotDriver.Type("fern");
                break;
            default:
                if (screen.IsDone)
                {
                    GD.Print("Bot: the plant is made");
                    return StepResult.Done;
                }

                return StepResult.Running;
        }

        _step++;
        return StepResult.Running;
    }
}

/// <summary>
/// Goes up to another player and does one thing with them: add them as a friend, send
/// a message, invite them to a party, or give them something.
/// </summary>
public sealed class MeetStep : BotStep
{
    private const float Close = 3f;

    private Player? _other;
    private Walker _walker = new Walker();
    private int _stage;
    private double _wait;

    public MeetStep()
        : base("meet someone", 60)
    {
    }

    public override void Begin(BotBody body)
    {
        _other = null;
        _walker = new Walker();
        _stage = 0;
        _wait = 0;
        Player? me = body.Me;

        if (me == null)
        {
            return;
        }

        List<Player> others = new List<Player>();

        foreach (Node node in me.GetParent().GetChildren())
        {
            Player? other = node as Player;

            if (other != null && other != me)
            {
                others.Add(other);
            }
        }

        if (others.Count > 0)
        {
            _other = others[body.Random.Next(others.Count)];
        }
    }

    public override StepResult Tick(BotBody body, double delta)
    {
        // Gone through a door: the body leaves the tree before it is freed.
        if (_other == null || !GodotObject.IsInstanceValid(_other) || !_other.IsInsideTree() || body.Me == null)
        {
            return StepResult.Failed;
        }

        switch (_stage)
        {
            case 0:
                StepResult walked = _walker.Walk(body, _other.GlobalPosition, Close, delta);

                if (walked == StepResult.Done)
                {
                    _stage = 1;
                }

                return walked == StepResult.Failed ? StepResult.Failed : StepResult.Running;
            case 1:
                Camera3D? camera = body.Me.GetViewport().GetCamera3D();
                Vector3 chest = _other.GlobalPosition + new Vector3(0f, 1f, 0f);

                if (camera == null || camera.IsPositionBehind(chest))
                {
                    return StepResult.Failed;
                }

                GD.Print("Bot: clicking on " + _other.DisplayName);
                BotDriver.Click(camera.UnprojectPosition(chest));
                _stage = 2;
                _wait = 0.6;
                return StepResult.Running;
            case 2:
                _wait -= delta;

                if (_wait > 0)
                {
                    return StepResult.Running;
                }

                return Act(body);
            case 4:
                // The chat opened on the conversation; type into it.
                _wait -= delta;

                if (_wait > 0)
                {
                    return StepResult.Running;
                }

                BotDriver.Type("hi");
                return StepResult.Done;
            default:
                _wait -= delta;

                if (_wait > 0)
                {
                    return StepResult.Running;
                }

                Button? giveOne = body.Usable(GivePanel.GiveGroup);

                if (giveOne != null)
                {
                    GD.Print("Bot: clicking Give 1");
                    body.Click(giveOne);
                }

                return StepResult.Done;
        }
    }

    // One of the target frame's buttons, whichever it picks among those shown.
    private StepResult Act(BotBody body)
    {
        List<Button> choices = new List<Button>();
        string[] groups = { TargetFrame.FriendGroup, TargetFrame.MessageGroup, TargetFrame.InviteGroup, TargetFrame.GiveGroup };

        foreach (string group in groups)
        {
            Button? button = body.Usable(group);

            if (button != null)
            {
                choices.Add(button);
            }
        }

        if (choices.Count == 0)
        {
            return StepResult.Failed;
        }

        Button pick = choices[body.Random.Next(choices.Count)];
        GD.Print("Bot: clicking " + pick.Text);
        body.Click(pick);

        if (pick.IsInGroup(TargetFrame.MessageGroup))
        {
            _stage = 4;
            _wait = 0.5;
            return StepResult.Running;
        }

        if (pick.IsInGroup(TargetFrame.GiveGroup))
        {
            _stage = 3;
            _wait = 0.6;
            return StepResult.Running;
        }

        return StepResult.Done;
    }
}
