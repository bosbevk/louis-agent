using System.Collections.Frozen;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Puls.Messages;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Shared.Turns;

namespace LouisAgent.Evals.Puls.Evals.Cook.Grading;

/// <summary>
/// Each turn's answer names the number of people the scenario asks for (<c>serves</c>, a number for the first turn or
/// one per turn). Not required: until the model check sees that the quantities scale, its job is to catch the answer
/// that forgot the number. A turn with nothing to read (it failed, or answered nothing) leaves it Unknown, never a
/// fail.
/// </summary>
/// <remarks>
/// Its <see cref="Entry"/> lets an eval file name it, <c>serves-as-asked</c> (Factory Method), as the general checks'
/// entries do; it takes no settings.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ServesAsAsked : ICheck
{
    public string Name => "serves-as-asked";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => false;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "serves" }.ToFrozenSet();

    /// <summary>Looks for the people asked for in each turn's answer.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown for a broken turn or one with nothing to read; a pass when nothing is asked or every turn names its
    /// number; else a fail saying what the turn named instead.
    /// </summary>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;

        var asked = CookExpectations.ServesOf(trial.Scenario);
        if (asked.Count == 0) return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ServesNotAsked));

        for (int i = 0; i < asked.Count; i++)
        {
            int number = i + 1;
            var view = i < turns.Count ? TurnView.From(turns[i]) : null;
            if (view is null || view.Error is not null || view.Answer.Length == 0)
                return Verdict.Unknown(Name, Version,
                    MessageText.Format(GradeMessage.ServesNothingToRead, new { Turn = number }));

            var found = Servings.Of(view.Answer);
            if (found.Contains(asked[i])) continue;
            return Verdict.Fail(Name, Version, found.Count == 0
                ? MessageText.Format(GradeMessage.ServesForgotten, new
                {
                    Turn = number,
                    Asked = asked[i]
                })
                : MessageText.Format(GradeMessage.ServesWrong,
                    new
                    {
                        Turn = number,
                        Found = string.Join(", ", found),
                        Asked = asked[i]
                    }));
        }

        return Verdict.Pass(Name, Version, MessageText.Format(GradeMessage.ServesRight));
    }

    /// <summary>What a file can set: nothing yet; the people are the scenario's.</summary>
    public sealed record Settings;

    /// <summary>The check as a file names it, <c>serves-as-asked</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("serves-as-asked", CheckKind.FixedRule)
    {
        /// <summary>Builds the check.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => new ServesAsAsked();
    }
}
