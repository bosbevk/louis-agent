using System.Globalization;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.AI.Evaluation;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading.Judges;

/// <summary>
/// What the cook was asked (F18 step 6.2), as the context the judge is handed: how many people the last answer is for, and the user's town. Built from the trial's scenario
/// (<see cref="Of"/>), so "a meal for four" has its four. The people are the last number the scenario asks for up to the last answer's turn (a number for the first turn, or one
/// per turn, carried forward when a turn asks for none). It carries no budget (6a.4f, dropped in 6a.3d): told the budget, the small judge judged the cost, which its prompt forbids and
/// another grader checks; a model can't misuse what it isn't told.
/// </summary>
public sealed class CookAsk(int? people, string? town) : EvaluationContext("cook-ask", Describe(people, town))
{
    public int? People { get; } = people;
    public string? Town { get; } = town;

    public static CookAsk Of(Trial trial)
    {
        var serves = CookExpectations.ServesOf(trial.Scenario);
        int? people = serves.Count == 0 ? null : serves[Math.Min(Math.Max(trial.Transcript.Turns.Count, 1), serves.Count) - 1];
        return new CookAsk(people, CookExpectations.TownOf(trial.Scenario));
    }

    // The judge is never told the budget: a small model judged the cost when it was.
    private static string Describe(int? people, string? town) => string.Join("\n",
        $"People: {(people is { } n ? n.ToString(CultureInfo.InvariantCulture) : "not said")}",
        $"Town: {(string.IsNullOrWhiteSpace(town) ? "not said" : town)}");
}
