using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Compare;

/// <summary>
/// The first answer's total, as the named reader reads it, is at most the amount the scenario expects under a key: a
/// basket within its budget. A scenario that sets no amount passes, since there is nothing to keep within; an answer
/// the reader reads no total from is Unknown; an amount that isn't a number above zero is refused.
/// </summary>
/// <remarks>
/// Extract, then compare (Hamcrest's FeatureMatcher, promptfoo's transform): the check holds the compare, the verdict
/// and the reasons, and an <see cref="IMoneyReader"/> (a Strategy, named in the file) holds how the domain writes a
/// total, so a domain ships a reader, not a check. It grades as the cook's <c>within-budget</c> did, step for step, so
/// that check can become this one over Puls's reader with its grades unchanged (F49.2b step 6): a streamed error is
/// <c>no-error</c>'s to report, not this one's. Its <see cref="Entry"/> builds it from a file's settings (Factory
/// Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class AmountUnderCheck(IMoneyReader reader, string key, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "amount-under";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { key };

    /// <summary>Reads the first answer's total and compares it with the scenario's amount.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown with no turn or a broken one, a pass when the scenario sets no amount, Unknown when no total is read,
    /// then a pass or a fail on the total against the amount.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Grade(Trial trial)
    {
        var first = trial.Transcript.Turns.FirstOrDefault();
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.AmountUnread));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (AmountIn(trial.Scenario) is not { } expected)
            return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.AmountNotSet, new { Key = key }));
        if (reader.Total(EngineEvents.AnswerOf(first)) is not { } total)
            return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.AmountUnread));

        var values = new
        {
            Total = total.ToString("0.00", CultureInfo.InvariantCulture),
            Expected = expected.ToString("0.##", CultureInfo.InvariantCulture)
        };
        return total <= expected
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.AmountWithin, values))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.AmountOver, values));
    }

    /// <summary>
    /// The amount the scenario expects under the key; null when it sets none. One that isn't a number above zero is
    /// refused, naming the scenario and the key.
    /// </summary>
    private decimal? AmountIn(EvalScenario scenario)
    {
        if (scenario.Expected.ValueKind != JsonValueKind.Object ||
            !scenario.Expected.TryGetProperty(key, out var value))
            return null;
        return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var amount) && amount > 0
            ? amount
            : throw new InvalidDataException(MessageText.Format(EvalErrorMessage.BadExpectedAmount, new
            {
                scenario.Id,
                Key = key
            }));
    }

    /// <summary>
    /// What a file can set: the scenario key holding the amount, the reader that reads the total, and whether the
    /// check must pass.
    /// </summary>
    public sealed record Settings(string Key, string Reader, bool Required = true);

    /// <summary>The check as a file names it, <c>amount-under</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("amount-under", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, through the money reader the file names; a blank key or reader is
        /// refused, as is a reader the catalogue doesn't have or one that reads no money.
        /// </summary>
        /// <remarks>Guard clauses, one blank setting each from the top (ADR 0051).</remarks>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            if (string.IsNullOrWhiteSpace(settings.Key)) throw Blank("key");
            if (string.IsNullOrWhiteSpace(settings.Reader)) throw Blank("reader");
            return new AmountUnderCheck(context.Readers.Get<IMoneyReader>(settings.Reader), settings.Key,
                settings.Required);
        }

        /// <summary>The refusal of a blank setting, naming the check type and the field.</summary>
        private InvalidDataException Blank(string field) =>
            new(MessageText.Format(CheckCatalogMessage.NoText, new
            {
                Type,
                Field = field
            }));
    }
}
