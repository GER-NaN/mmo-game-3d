namespace MmoGame3d.Dev;

using MmoGame3d.Dev.Activities;

/// <summary>Every persona there is. Add one here, and to scripts/bots-up.ps1's mix.</summary>
public static class BotPersonas
{
    public const string Default = "wanderer";

    public static BotPersona Get(string name)
    {
        switch (name)
        {
            case "curious":
                return Curious();
            case "gamer":
                return Gamer();
            case "escaper":
                return Escaper();
            case "wedger":
                return Wedger();
            case "earner":
                return Earner();
            case "slow":
                return Slow();
            case "masher":
                return Masher();
            case "dropper":
                return Dropper();
            case "shadow":
                return Shadow();
            case "eventer":
                return Eventer();
            default:
                return new BotPersona(Default, "I walk around and do a bit of everything.");
        }
    }

    // Opens everything and clicks around in it: breadth, with no code per screen.
    private static BotPersona Curious()
    {
        BotPersona p = new BotPersona("curious", "I open everything and press what I find.");
        p.Own.Add(PersonaActivities.PokeAround);
        p.Own.Add(PersonaActivities.PokeAtTerminal);
        p.Own.Add(PersonaActivities.SaySomethingOdd);
        p.Likes["walk around town"] = 0.3;
        p.Likes["fight drones"] = 0.3;
        return p;
    }

    // Plays the mini games, over and over.
    private static BotPersona Gamer()
    {
        BotPersona p = new BotPersona("gamer", "I play the terminal games all day.");
        p.Likes["play Agent Defense"] = 6;
        p.Likes["use a public terminal"] = 4;
        p.Likes["make a house plant"] = 4;
        p.Likes["walk around town"] = 0.3;
        p.Likes["meet someone"] = 0.3;
        p.ChainShare = 40;
        p.JoinChance = 0.1;
        return p;
    }

    // Looks for the way out of the world.
    private static BotPersona Escaper()
    {
        BotPersona p = new BotPersona("escaper", "I look for the edge of the world and try to get past it.");
        p.Own.Add(PersonaActivities.RunForTheEdge);
        p.Likes["walk the meadows"] = 3;
        p.Likes["go to the outskirts"] = 3;
        p.JoinChance = 0;
        return p;
    }

    // Squeezes into the gaps between buildings, to find where players get wedged.
    private static BotPersona Wedger()
    {
        BotPersona p = new BotPersona("wedger", "I squeeze into gaps to see where I get stuck.");
        p.Own.Add(PersonaActivities.SqueezeIntoAGap);
        p.Likes["walk around town"] = 0.5;
        p.JoinChance = 0;
        return p;
    }

    // Money, then more money.
    private static BotPersona Earner()
    {
        BotPersona p = new BotPersona("earner", "I pick things up and sell them, as much as I can.");
        p.Likes["earn some money"] = 8;
        p.Likes["recycle something"] = 4;
        p.Likes["fight drones"] = 2;
        p.Likes["walk around town"] = 0.3;
        p.ChainShare = 50;
        p.CancelChance = 0.1;
        return p;
    }

    // Hammers keys nobody would press in that order: input the game did not plan for.
    private static BotPersona Masher()
    {
        BotPersona p = new BotPersona("masher", "I press all the keys, fast, in any order.");
        p.Own.Add(PersonaActivities.MashKeys);
        p.Likes["walk around town"] = 0.5;
        p.AsideShare = 25;
        p.AsideRate = 3;
        return p;
    }

    // A wanderer whose connection drops in the middle of things, most of all where the
    // server holds state for the player: a ride, a terminal, a game.
    private static BotPersona Dropper()
    {
        BotPersona p = new BotPersona("dropper", "My connection drops at the worst moments.");
        p.CutChance = 0.3;
        p.Likes["ride a robo taxi"] = 3;
        p.Likes["use a public terminal"] = 3;
        p.Likes["play Agent Defense"] = 3;
        p.Likes["make a house plant"] = 2;
        return p;
    }

    // Follows someone and does what they do, where they do it.
    private static BotPersona Shadow()
    {
        BotPersona p = new BotPersona("shadow", "I follow someone and use what they use.");
        p.Own.Add(PersonaActivities.ShadowSomeone);
        p.Likes["walk around town"] = 0.3;
        p.JoinChance = 0.6;
        return p;
    }

    // Checks for world events often, and goes to half of those it finds running.
    private static BotPersona Eventer()
    {
        BotPersona p = new BotPersona("eventer", "I check for world events and go when one is on.");
        p.Likes["check world events"] = 8;
        p.Likes["wear an EMP emitter"] = 4;
        p.Likes["walk around town"] = 0.5;
        return p;
    }

    // A wanderer at a third of the pace that sticks with what it starts: slow players,
    // and a bot easy to follow on screen.
    private static BotPersona Slow()
    {
        BotPersona p = new BotPersona("slow", "I take my time and finish what I start.");
        p.Pace = 3;
        p.CancelChance = 0.05;
        p.AsideRate = 0.5;
        return p;
    }
}
