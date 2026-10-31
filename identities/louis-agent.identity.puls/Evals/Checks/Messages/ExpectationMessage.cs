using System.ComponentModel;

namespace LouisAgent.Identities.Puls.Evals.Checks.Messages;

/// <summary>
/// What you're told when a scenario's <c>expected</c> asks for something the checks can't read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
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

    [Description("Scenario '{Id}': 'town' in expected must be a town's name.")]
    BadTown,
}
