using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Answers;

/// <summary>
/// The first turn's answer, with its leading and trailing spaces trimmed, starts with the text, case ignored: an
/// answer that must open with a title or a greeting. A turn the infrastructure broke is Unknown; a turn that streamed
/// an error fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler): one plain rule, a string's start, never a pattern (ADR 0057).
/// Its <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class StartsWithCheck(string text, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "starts-with";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Reads how the first turn's answer opens, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a pass or
    /// a fail on how the trimmed answer starts.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var answer = EngineEvents.AnswerOf(first).Trim();
        return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(answer, text, CompareOptions.IgnoreCase)
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.StartsWithPass, new { Text = text }))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.StartsOtherwise, new { Text = text }));
    }

    /// <summary>What a file can set: the text the answer must open with, and whether the check must pass.</summary>
    public sealed record Settings(string Text, bool Required = true);

    /// <summary>The check as a file names it, <c>starts-with</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("starts-with", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, refusing a blank text: every answer would pass.
        /// </summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            !string.IsNullOrWhiteSpace(settings.Text)
                ? new StartsWithCheck(settings.Text, settings.Required)
                : throw Refused(CheckCatalogMessage.NoText, "text");
    }
}
