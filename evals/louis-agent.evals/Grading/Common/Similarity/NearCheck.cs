using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Similarity;

/// <summary>
/// The whole first answer, trimmed and case ignored, is at most a number of edits from the text: a letter added,
/// removed or changed is one edit. For a short fixed answer (a label, a yes or no, a code), where a typo or a full
/// stop shouldn't fail it; a long answer wants <c>contains</c> or the judge. A turn the infrastructure broke is
/// Unknown; a turn that streamed an error fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler) with the Levenshtein distance, worked out in code with the
/// textbook two-row table (Wagner and Fischer), Louis's pick over a fuzzy "contains" or a line-by-line match. Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class NearCheck(string text, int distance, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "near";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>
    /// Measures how far the first turn's answer is from the text, after making sure that turn can be judged.
    /// </summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a pass or
    /// a fail on the edits, which the reason names.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var edits = Edits(EngineEvents.AnswerOf(first).Trim().ToLowerInvariant(), text.Trim().ToLowerInvariant());
        var holes = new
        {
            Edits = edits,
            Text = text,
            Distance = distance
        };
        return edits <= distance
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.NearPass, holes))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.NearFar, holes));
    }

    /// <summary>
    /// The fewest letters to add, remove or change to turn one text into the other (the Levenshtein distance).
    /// </summary>
    private static int Edits(string from, string to)
    {
        // Row by row over the table: the cost of turning each start of `from` into each start of `to`; only the row
        // before is needed. The first row is turning nothing into each start of `to`: one addition a letter.
        var before = Enumerable.Range(0, to.Length + 1).ToArray();
        var row = new int[to.Length + 1];
        for (var i = 1; i <= from.Length; i++)
        {
            row[0] = i;
            for (var j = 1; j <= to.Length; j++)
            {
                var change = from[i - 1] == to[j - 1] ? 0 : 1;
                row[j] = Math.Min(Math.Min(before[j] + 1, row[j - 1] + 1), before[j - 1] + change);
            }

            (before, row) = (row, before);
        }

        return before[to.Length];
    }

    /// <summary>
    /// What a file can set: the text the answer should be, how many edits away it may be, and whether the check must
    /// pass.
    /// </summary>
    public sealed record Settings(string Text, int Distance, bool Required = true);

    /// <summary>The check as a file names it, <c>near</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("near", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, refusing a blank text (the rule would be about nothing) and a distance
        /// under zero (no answer could pass).
        /// </summary>
        /// <remarks>Guard clauses, one bad setting each from the top (ADR 0051).</remarks>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            if (string.IsNullOrWhiteSpace(settings.Text))
                throw Refusal(CheckCatalogMessage.NoText, "text");
            if (settings.Distance < 0)
                throw Refusal(CheckCatalogMessage.BelowZero, "distance");
            return new NearCheck(settings.Text, settings.Distance, settings.Required);
        }

        /// <summary>A refusal of one setting, naming the check type and the field.</summary>
        private InvalidDataException Refusal(CheckCatalogMessage message, string field) =>
            new(MessageText.Format(message, new
            {
                Type,
                Field = field
            }));
    }
}
