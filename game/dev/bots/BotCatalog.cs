namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using MmoGame3d.Dev.Activities;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.World;

/// <summary>
/// Everything a bot can do: activities (chosen by weight, or asides on a timer) and chains
/// (related, random, and goals), from the activity classes and every IBotFeature.
/// Activities with weight 0 are only started by chains and goals.
/// </summary>
public sealed class BotCatalog
{
    // The driver's own: out of a trap, out of a party before a goal, home to town.
    public static readonly BotActivity Escape = new EscapeActivity();
    public static readonly BotActivity LeaveParty = new LeavePartyActivity();
    public static readonly BotActivity BackToTown = new BackToTownActivity();

    private static BotCatalog? _all;

    public List<BotActivity> Activities { get; } = new List<BotActivity>();

    public List<BotChain> Chains { get; } = new List<BotChain>();

    public static BotCatalog All
    {
        get
        {
            if (_all == null)
            {
                _all = new BotCatalog();
                _all.AddActivities();
                _all.AddChains();

                foreach (Type type in typeof(IBotFeature).Assembly.GetTypes())
                {
                    if (typeof(IBotFeature).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        ((IBotFeature)Activator.CreateInstance(type)!).AddTo(_all);
                    }
                }
            }

            return _all;
        }
    }

    // The activities a bot may choose when it is free, for random chains.
    public List<BotActivity> Choosable
    {
        get
        {
            List<BotActivity> choosable = new List<BotActivity>();

            foreach (BotActivity activity in Activities)
            {
                if (activity.Timing == BotTiming.Chosen && activity.Weight > 0)
                {
                    choosable.Add(activity);
                }
            }

            return choosable;
        }
    }

    public void Add(BotActivity activity)
    {
        Activities.Add(activity);
    }

    public void Add(BotChain chain)
    {
        Chains.Add(chain);
    }

    private void AddActivities()
    {
        // Chosen when free, by weight.
        Add(new WalkAroundTownActivity());
        Add(new TalkToTownspersonActivity());
        Add(new VisitCollegeActivity());
        Add(new GoShoppingActivity());
        Add(new UsePublicTerminalActivity());
        Add(new UsePhoneActivity());
        Add(new PlayDefenseActivity());
        Add(new CheckBagActivity());
        Add(new TidyBagActivity());
        Add(new RideTaxiActivity());
        Add(new FixSomethingActivity());
        Add(new RepairLightsActivity());
        Add(new TagSubwayActivity());
        Add(new OutskirtsActivity());
        Add(new WalkMeadowsActivity());
        Add(new CheckWorldEventsActivity());
        Add(new SwitchCharacterActivity());
        Add(new GreenhouseActivity());
        Add(new MeetSomeoneActivity());
        Add(LeaveParty);
        Add(new RecycleActivity());
        Add(new LookAtMapActivity());
        Add(new LookAtSkillsActivity());
        Add(new ExploreActivity());
        Add(BackToTown);

        // Asides, on their own timers too.
        Add(new EmoteActivity());

        // Only chains and goals start these.
        Add(new HuntDroneActivity());
        Add(new ReportDronesActivity());
        Add(new BuyActivity(ItemType.Battery));
        Add(new BuyActivity(ItemType.EmpEmitter));
        Add(new SwapBatteryActivity());
        Add(new RecycleActivity());
        Add(new PickUpActivity());
        Add(new EquipActivity(ItemType.Phone));
        Add(new EquipActivity(ItemType.EmpEmitter));
        Add(new EnrollActivity(CareerId.MechanicalEngineer));
        Add(new EnrollActivity(CareerId.ComputerScientist));
    }

    private static CareerId Other(BotBody body)
    {
        return body.Career == (int)CareerId.ComputerScientist ? CareerId.MechanicalEngineer : CareerId.ComputerScientist;
    }

    private void AddChains()
    {
        // Goals: the facts wanted, planned by BotResolver, then what they were for.
        Add(new GoalChain("fight drones", 4, body => BotWhere.InWorld(body) && body.LiveDrones().Count > 0,
            body => new List<BotFact> { BotFact.Wears(ItemType.EmpEmitter) }, 12, 300,
            body => new HuntDroneActivity(),
            body => new HuntDroneActivity()));
        Add(new GoalChain("charge the phone", 5, body => body.Zone != null && body.PhonePercent >= 0 && body.PhonePercent < 20,
            body => new List<BotFact> { BotFact.PhoneAtLeast(50) }, 8, 240));
        Add(new GoalChain("earn some money", 2, body => body.Zone != null,
            body => new List<BotFact> { BotFact.MoneyAtLeast(body.Money + 10) }, 8, 240));
        Add(new GoalChain("wear an EMP emitter", 2, body => body.Zone != null && !body.Wears(ItemType.EmpEmitter),
            body => new List<BotFact> { BotFact.Wears(ItemType.EmpEmitter) }, 8, 240));
        Add(new GoalChain("become a computer scientist", 1, body => body.Zone != null && body.Career != (int)CareerId.ComputerScientist,
            body => new List<BotFact> { BotFact.Career(CareerId.ComputerScientist) }, 4, 120));
        Add(new GoalChain("become a mechanical engineer", 1, body => body.Zone != null && body.Career != (int)CareerId.MechanicalEngineer,
            body => new List<BotFact> { BotFact.Career(CareerId.MechanicalEngineer) }, 4, 120));

        // Related: orders around one piece of state that random picks would seldom reach.
        // From whichever career it has (none counts as the engineer's side): the other,
        // then back, then the other again.
        Add(new RelatedChain("change careers and back", 1,
            body => new EnrollActivity(Other(body)),
            body => new EnrollActivity(Other(body)),
            body => new EnrollActivity(Other(body))));
        Add(new RelatedChain("buy, recycle, buy", 1,
            body => new BuyActivity(ItemType.Battery),
            body => new RecycleActivity(),
            body => new BuyActivity(ItemType.Battery)));
        Add(new RelatedChain("two plants running", 1,
            body => new GreenhouseActivity(),
            body => new GreenhouseActivity()));
        Add(new RelatedChain("travel the long way", 1,
            body => new TravelActivity(ZoneIds.Greenhouse),
            body => new TravelActivity(ZoneIds.Subway),
            body => new TravelActivity(ZoneIds.Meadows),
            body => new TravelActivity(ZoneIds.Shop)));

        // Random: three to six of the choosable, drawn afresh each time.
        Add(new RandomChain("random chain", 3, () => Choosable));
    }
}
