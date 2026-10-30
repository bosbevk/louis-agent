using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Tools;

/// <summary>
/// A tool with this name was called somewhere in the trial, by the agent or by a specialist it handed to, the way
/// Anthropic's tool-call checks ask "was this tool used". The name must match exactly.
/// </summary>
/// <remarks>
/// A specialist's call counts, because an eval asks whether the work was done, not who did it; the reason says
/// which. A streamed error is <c>no-error</c>'s to report, so a call made before one still passes. A call already
/// found passes even when a later turn broke; with no call found, a broken turn makes it Unknown, since the call
/// might have been in it. Its <see cref="Entry"/> builds it from a file's settings (Factory Method), nested here as
/// the other general checks' are.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ToolUsedCheck(string tool, bool required = true) : ICheck
{
    public string Name => "tool-used";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Looks for the call in every turn, the agent's own calls first.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Look(trial.Transcript.Turns));

    /// <summary>
    /// A pass naming the first turn where the agent, or else a specialist, called the tool; Unknown when there was
    /// none and a turn broke; otherwise a fail naming the tool.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Look(IReadOnlyList<TurnTranscript> turns)
    {
        if (turns.FirstOrDefault(turn => EngineEvents.ToolsCalled(turn).Contains(tool)) is { } own)
            return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ToolUsed, new
            {
                Tool = tool,
                own.Turn
            }));
        if (turns.FirstOrDefault(turn => EngineEvents.SpecialistToolsCalled(turn).Contains(tool)) is { } handed)
            return Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.ToolUsedBySpecialist, new
                {
                    Tool = tool,
                    handed.Turn
                }));
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ToolNotUsed, new { Tool = tool }));
    }

    /// <summary>What a file can set: the tool's name, and whether the check must pass.</summary>
    public sealed record Settings(string Tool, bool Required = true);

    /// <summary>The check as a file names it, <c>tool-used</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("tool-used", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            new ToolUsedCheck(settings.Tool, settings.Required);
    }
}
