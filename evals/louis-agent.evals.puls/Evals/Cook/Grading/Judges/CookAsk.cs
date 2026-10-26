using System.Globalization;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;

/// <summary>
/// What the cook was asked (F18 step 6.2), as the context the judge is handed: how many people the last answer is for, the rand budget, and the user's town. Built from the trial's
/// scenario (<see cref="Of"/>), so "a meal for four" has its four. The people are the last number the scenario asks for up to the last answer's turn (a number for the first turn, or one
/// per turn, carried forward when a turn asks for none). The budget is context only: the judge never judges prices.
/// </summary>
public sealed class CookAsk(int? people, decimal? budget, string? town) : EvaluationContext("cook-ask", Describe(people, budget, town))
{
    public int? People { get; } = people;
    public decimal? Budget { get; } = budget;
    public string? Town { get; } = town;

    public static CookAsk Of(Trial trial)
    {
        var serves = CookExpectations.ServesOf(trial.Scenario);
        int? people = serves.Count == 0 ? null : serves[Math.Min(Math.Max(trial.Transcript.Turns.Count, 1), serves.Count) - 1];
        return new CookAsk(people, CookExpectations.BudgetOf(trial.Scenario), CookExpectations.TownOf(trial.Scenario));
    }

    private static string Describe(int? people, decimal? budget, string? town) => string.Join("\n",
        $"People: {(people is { } n ? n.ToString(CultureInfo.InvariantCulture) : "not said")}",
        $"Budget: {(budget is { } rand ? "R" + rand.ToString("0.##", CultureInfo.InvariantCulture) : "none")}",
        $"Town: {(string.IsNullOrWhiteSpace(town) ? "not said" : town)}");
}
