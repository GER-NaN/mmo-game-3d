namespace MmoGame3d.Rules.Social;
/// <summary>
/// Whois, a player's page in the terminal (docs/features/social-page-concept.md): who
/// they are, their career, where they are and what they are doing, their Plan, their
/// skills if they show them, and props. What the owner sets is a WhoisSettings; the
/// rest is read from the game.
/// </summary>
public class WhoisSettings
{
    public const int MaxPlanLength = 140;

    public string Plan { get; set; } = "";
    public bool ShowSkills { get; set; }
    public bool ShowLocation { get; set; } = true;
}
