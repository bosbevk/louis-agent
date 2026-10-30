using System.Globalization;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Identities.Puls.Judges;

/// <summary>
/// What the cook was asked, handed to the judge: how many people the last answer is for, and the user's town, so "a
/// meal for four" has its four. The people are the last number the scenario asks for up to the last answer's turn.
/// </summary>
/// <remarks>
/// It never carries the budget: told the budget, the small judge judged the cost, which its prompt forbids and another
/// check already covers. A model can't misuse what it isn't told.
/// </remarks>
public sealed class CookAsk(int? people, string? town) : EvaluationContext("cook-ask", Describe(people, town))
{
    /// <summary>How many people the last answer should feed, if the scenario says.</summary>
    public int? People { get; } = people;

    /// <summary>Where the user is, if the scenario says.</summary>
    public string? Town { get; } = town;

    /// <summary>
    /// Reads the ask from a trial: a number for the first turn, or one per turn, carried forward when a turn asks for
    /// none.
    /// </summary>
    public static CookAsk Of(Trial trial)
    {
        var serves = CookExpectations.ServesOf(trial.Scenario);
        int? people = serves.Count == 0
            ? null
            : serves[Math.Min(Math.Max(trial.Transcript.Turns.Count, 1), serves.Count) - 1];
        return new CookAsk(people, CookExpectations.TownOf(trial.Scenario));
    }

    /// <summary>The ask as the judge reads it: the people and the town, one line each.</summary>
    private static string Describe(int? people, string? town) => string.Join("\n",
        $"People: {(people is { } n ? n.ToString(CultureInfo.InvariantCulture) : "not said")}",
        $"Town: {(string.IsNullOrWhiteSpace(town) ? "not said" : town)}");
}
