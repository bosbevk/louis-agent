using System.ComponentModel;

namespace LouisAgent.Identities.Puls.Evals.Checks.Messages;

/// <summary>The refusals for a scenario whose <c>expected</c> asks for something unreadable (ADR 0009: a text is a message enum member).</summary>
public enum ExpectationMessage
{
    [Description(
        "Scenario '{Id}' needs 'routes_to' in expected: the role its first turn goes to, or null when Puls answers it itself.")]
    NoRoute,

    [Description("Scenario '{Id}': 'routes_to' in expected must be a role's name or null.")]
    BadRoute,

    [Description(
        "Scenario '{Id}': 'serves' in expected must be a whole number of people above 0, or one for each turn.")]
    BadServes,

    [Description("Scenario '{Id}': 'budget' in expected must be a rand amount above 0.")]
    BadBudget,

    [Description("Scenario '{Id}': 'shop' in expected must be one of: {Supported}.")]
    BadShop,

    [Description(
        "Scenario '{Id}' expects a shop to price at, so it needs 'town' in expected: the town the cook names as the area of every shop call.")]
    NoTown,

    [Description("Scenario '{Id}': 'town' in expected must be a town's name.")]
    BadTown,
}
