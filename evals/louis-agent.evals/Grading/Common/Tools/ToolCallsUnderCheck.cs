using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Tools;

/// <summary>
/// Every tool call in the trial, the agent's and its specialists', hand-overs included, stays under the limit: the
/// whole of the work the task took. The reason says the total and the limit. A turn the infrastructure broke makes it
/// Unknown.
/// </summary>
/// <remarks>
/// A run fact over the calls <c>tool-used</c> reads. Every call counts, the engine's own StartSubSession and
/// ContinueSubSession too (Louis's pick), so a delegating mode spends one call on each hand-over. Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ToolCallsUnderCheck(int count, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "tool-calls-under";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>
    /// Counts every call in every turn and compares the total with the limit, unless a turn was broken.
    /// </summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken)
    {
        if (Verdict.InfrastructureFailure(Name, Version, trial.Transcript.Turns) is { } unknown)
            return ValueTask.FromResult(unknown);

        var calls = trial.Transcript.Turns.Sum(turn =>
            EngineEvents.ToolsCalled(turn).Count + EngineEvents.SpecialistToolsCalled(turn).Count);
        var measure = new
        {
            Calls = calls,
            Limit = count
        };
        return ValueTask.FromResult(calls < count
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ToolCallsUnderPass, measure))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ToolCallsOver, measure)));
    }

    /// <summary>What a file can set: the limit in calls, and whether the check must pass.</summary>
    public sealed record Settings(int Count, bool Required = true);

    /// <summary>The check as a file names it, <c>tool-calls-under</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("tool-calls-under", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a limit under one: no trial could pass.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => settings.Count >= 1
            ? new ToolCallsUnderCheck(settings.Count, settings.Required)
            : throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.BelowOne, new
            {
                Type,
                Field = "count"
            }));
    }
}
