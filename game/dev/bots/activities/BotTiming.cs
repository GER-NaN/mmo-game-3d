namespace MmoGame3d.Dev;
// How an activity is picked: chosen by weight when the bot is free, or an aside, which
// also fires on a timer and may pause whatever runs (BotDriver).
public enum BotTiming
{
    Chosen,
    Aside,
}
