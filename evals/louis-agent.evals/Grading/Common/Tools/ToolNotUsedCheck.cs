using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Tools;

/// <summary>
/// No one in the trial called the tool, not the agent and not a specialist it handed to: a tool the task must do
/// without. A call fails, naming the turn and who made it; with no call found, a broken turn makes it Unknown, since
/// the call might have been in it.
/// </summary>
/// <remarks>
/// The negation of <c>tool-used</c>, reading the same calls the same way, so the two can't disagree about what was
/// called. Its <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ToolNotUsedCheck(string tool, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "tool-not-used";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Looks for a call in every turn, the agent's own calls first.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Look(trial.Transcript.Turns));

    /// <summary>
    /// A fail naming the first turn where the agent, or else a specialist, called the tool; Unknown when there was
    /// none and a turn broke; otherwise a pass.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Look(IReadOnlyList<TurnTranscript> turns)
    {
        if (turns.FirstOrDefault(turn => EngineEvents.ToolsCalled(turn).Contains(tool)) is { } own)
            return Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.ToolUsedForbidden, new
                {
                    Tool = tool,
                    own.Turn
                }));
        if (turns.FirstOrDefault(turn => EngineEvents.SpecialistToolsCalled(turn).Contains(tool)) is { } handed)
            return Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.ToolUsedBySpecialistForbidden, new
                {
                    Tool = tool,
                    handed.Turn
                }));
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ToolNotUsed, new { Tool = tool }));
    }

    /// <summary>What a file can set: the tool's name, and whether the check must pass.</summary>
    public sealed record Settings(string Tool, bool Required = true);

    /// <summary>The check as a file names it, <c>tool-not-used</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("tool-not-used", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a blank tool: no call could match it.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            !string.IsNullOrWhiteSpace(settings.Tool)
                ? new ToolNotUsedCheck(settings.Tool, settings.Required)
                : throw Refused(CheckCatalogMessage.NoText, "tool");
    }
}
