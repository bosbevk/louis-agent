using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Answers;

/// <summary>
/// The first turn's answer contains none of the texts, with case and accents ignored: a phrase the agent must never
/// say ("as an AI"), or a name it must leave out. A turn the infrastructure broke is Unknown; a turn that streamed an
/// error fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler): one plain rule a trial satisfies or not, with no pattern
/// (ADR 0057), the negation of <c>contains</c>. Its <see cref="Entry"/> builds it from a file's settings (Factory
/// Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class NotContainsCheck(IReadOnlyList<string> texts, bool required = true) : ICheck
{
    /// <summary>Ignore case and accents: a letter with a mark matches the same letter without one.</summary>
    private const CompareOptions Loosely = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <inheritdoc />
    public string Name => "not-contains";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Looks for the texts in the first turn's answer, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a fail
    /// naming the first text found, or a pass.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var answer = EngineEvents.AnswerOf(first);
        var found = texts.FirstOrDefault(text =>
            CultureInfo.InvariantCulture.CompareInfo.IndexOf(answer, text, Loosely) >= 0);
        return found is null
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ContainsNone, new { texts.Count }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.ContainsForbidden, new { Text = found }));
    }

    /// <summary>What a file can set: the texts the answer must not hold, and whether the check must pass.</summary>
    public sealed record Settings(IReadOnlyList<string> Texts, bool Required = true);

    /// <summary>The check as a file names it, <c>not-contains</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("not-contains", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, refusing an empty list: every answer would pass.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context) => settings.Texts.Count > 0
            ? new NotContainsCheck(settings.Texts, settings.Required)
            : throw Refused(CheckCatalogMessage.NothingListed, "texts");
    }
}
