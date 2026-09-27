namespace MmoGame3d.Dev;

using System;
using System.Collections.Generic;
using MmoGame3d.Dev.Activities;
using MmoGame3d.Rules.Items;
using MmoGame3d.Rules.Skills;
using MmoGame3d.Rules.World;

/// <summary>
/// A game feature's part in bot testing, in one file: the activities and chains that play
/// it. Every class that implements this is found when bots start (BotCatalog) and adds
/// its own; nothing else needs changing. See game/dev/features/.
/// </summary>
public interface IBotFeature
{
    void AddTo(BotCatalog catalog);
}

/// <summary>
/// Everything a bot can do: activities (chosen by weight, or asides on a timer) and chains
/// (related, random, and goals), from BotActivities, BotGoals, the activity classes and
/// every IBotFeature. Activities with weight 0 are only started by chains and goals.
/// </summary>
public sealed class BotCatalog
{
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
                _all.Activities.AddRange(BotActivities.All);
                _all.AddModelActivities();

                foreach (BotGoal goal in BotGoals.All)
                {
                    _all.Add(goal);
                }

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

    // An older hand-coded goal, run as a chain.
    public void Add(BotGoal goal)
    {
        Chains.Add(new LegacyGoalChain(goal));
    }

    public void Add(BotChain chain)
    {
        Chains.Add(chain);
    }

    private void AddModelActivities()
    {
        Add(new GreenhouseActivity());
        Add(new EmoteActivity());
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
        // Goals: the facts wanted, planned by BotResolver.
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
