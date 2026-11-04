using LouisAgent.Choices;

namespace LouisAgent.Identities.Puls;

/// <summary>
/// The roles Puls offers a session (ADR 0043: each identity keeps its own enum of the names it uses): its
/// specialists, <c>specialists/puls-*.md</c>, and the minion roles the cook hands work to,
/// <c>minions/puls-minion-*.md</c>. A member's wire name is the role's file name; hosts and the eval protocol carry
/// it as text.
/// </summary>
public enum PulsRole
{
    /// <summary>The cook, who turns a meal request into a recipe, a list and where to buy.</summary>
    [WireName("puls-cook")] Cook,

    /// <summary>The researcher.</summary>
    [WireName("puls-researcher")] Researcher,

    /// <summary>The analyst.</summary>
    [WireName("puls-analyst")] Analyst,

    /// <summary>The historian.</summary>
    [WireName("puls-historian")] Historian,

    /// <summary>The reader.</summary>
    [WireName("puls-reader")] Reader,

    /// <summary>The minion that finds one ingredient's price at each named shop.</summary>
    [WireName("puls-minion-web-price-lookup")]
    MinionWebPriceLookup,

    /// <summary>The minion that searches the recipe sites for one dish and answers with one link.</summary>
    [WireName("puls-minion-find-recipe")] MinionFindRecipe,
}
