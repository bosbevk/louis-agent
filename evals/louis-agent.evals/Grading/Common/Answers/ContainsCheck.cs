using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Answers;

/// <summary>
/// The first turn's answer contains at least one of the texts, with case and accents ignored, so "resep" finds
/// "Resep" and "creme" finds "crème". A turn the infrastructure broke is Unknown; a turn that streamed an error fails.
/// </summary>
/// <remarks>
/// It takes a list of plain texts and no pattern: a list covers "one of these words", which is what an eval asks
/// for, while a pattern is a language to learn, a slow-match risk and hard to read back (ADR 0057). promptfoo's
/// <c>icontains-any</c> and Inspect's <c>includes()</c> work the same way. Its <see cref="Entry"/> builds it from a
/// file's settings (Factory Method), nested here as the other general checks' are.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class ContainsCheck(IReadOnlyList<string> texts, bool required = true) : ICheck
{
    /// <summary>Ignore case and accents: a letter with a mark matches the same letter without one.</summary>
    private const CompareOptions Loosely = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public string Name => "contains";
    public int Version => 1;
    public CheckKind Kind => CheckKind.FixedRule;
    public bool Required => required;
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Looks for the texts in the first turn's answer, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a pass
    /// naming the first text found, or a fail saying how many were looked for.
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
            ? Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ContainsNone, new { texts.Count }))
            : Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.ContainsPass, new { Text = found }));
    }

    /// <summary>What a file can set: the texts to look for, and whether the check must pass.</summary>
    public sealed record Settings(IReadOnlyList<string> Texts, bool Required = true);

    /// <summary>The check as a file names it, <c>contains</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("contains", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, refusing an empty list: there would be nothing to find.
        /// </summary>
        protected override ICheck Create(Settings settings) => settings.Texts.Count > 0
            ? new ContainsCheck(settings.Texts, settings.Required)
            : throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.NothingListed, new
            {
                Type,
                Field = "texts"
            }));
    }
}
