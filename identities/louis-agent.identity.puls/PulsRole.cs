using LouisAgent.Choices;


namespace LouisAgent.Identities.Puls;

/// <summary>
/// The roles Puls offers a session (ADR 0043: each identity keeps its own enum of the names it uses): its specialists, <c>specialists/puls-*.md</c>, and the minion role
/// the cook's price lookup runs as. A member's wire name is the role's file name; hosts and the eval protocol carry it as text.
/// </summary>
public enum PulsRole
{
    [WireName("puls-cook")] Cook,
    [WireName("puls-researcher")] Researcher,
    [WireName("puls-analyst")] Analyst,
    [WireName("puls-historian")] Historian,
    [WireName("puls-reader")] Reader,
    [WireName("puls-minion-web-price-lookup")] MinionWebPriceLookup,
}
