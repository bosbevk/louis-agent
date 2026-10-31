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
/// Each turn's answer names the number the scenario expects under a key, as the named reader reads it: one number for
/// the first turn, or a list with one per turn, as "a meal for 4, then for 6". A scenario that asks for none passes; a
/// turn asked for a number that has no answer to read is Unknown; an expected number that isn't a whole number of at
/// least one is refused.
/// </summary>
/// <remarks>
/// Extract, then compare, as <see cref="AmountUnderCheck"/> is: an <see cref="INumberReader"/> (a Strategy, named in
/// the file) holds how the domain writes a number, in digits or in its words. It grades as the cook's own
/// <c>serves-as-asked</c> did, step for step, so the cook names it under that name, over Puls's reader, with its
/// grades unchanged. Its <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class NumberNamedCheck(INumberReader reader, string key, bool required = true) : ICheck
{
    /// <inheritdoc />
    public string Name => "number-named";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { key };

    /// <summary>Reads each asked turn's answer for its number.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Grade(trial));

    /// <summary>
    /// Unknown when a turn broke, a pass when nothing is asked, then turn by turn: Unknown when a turn has nothing to
    /// read, a fail naming the turn when it doesn't name its number, and a pass when every turn does.
    /// </summary>
    /// <remarks>Guard clauses, then one guard per asked turn (ADR 0051).</remarks>
    private Grade Grade(Trial trial)
    {
        var turns = trial.Transcript.Turns;
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;

        var asked = NumbersIn(trial.Scenario);
        if (asked.Count == 0)
            return Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.NumberNotAsked, new { Key = key }));

        for (var i = 0; i < asked.Count; i++)
        {
            var turn = i + 1;
            var answer = i < turns.Count && !EngineEvents.StreamedError(turns[i])
                ? EngineEvents.AnswerOf(turns[i])
                : "";
            if (answer.Length == 0)
                return Verdict.Unknown(Name, Version,
                    MessageText.Format(CommonGradeMessage.NumberNothingToRead, new { Turn = turn }));

            var found = reader.Numbers(answer);
            if (found.Contains(asked[i])) continue;
            return Verdict.Fail(Name, Version, found.Count == 0
                ? MessageText.Format(CommonGradeMessage.NumberForgotten, new
                {
                    Turn = turn,
                    Asked = asked[i]
                })
                : MessageText.Format(CommonGradeMessage.NumberWrong, new
                {
                    Turn = turn,
                    Found = string.Join(", ", found),
                    Asked = asked[i]
                }));
        }

        return Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.NumberNamedPass));
    }

    /// <summary>
    /// The numbers the scenario asks for, one per turn from the first; none when it sets no key. A value that isn't a
    /// whole number of at least one, or a list of them, is refused, naming the scenario and the key.
    /// </summary>
    /// <remarks>A guard clause for the missing key, then a switch expression on the value's kind (ADR 0051).</remarks>
    private IReadOnlyList<int> NumbersIn(EvalScenario scenario)
    {
        if (scenario.Expected.ValueKind != JsonValueKind.Object ||
            !scenario.Expected.TryGetProperty(key, out var value))
            return [];
        List<JsonElement> written = value.ValueKind switch
        {
            JsonValueKind.Number => [value],
            JsonValueKind.Array => [.. value.EnumerateArray()],
            _ => []
        };
        if (written.Count == 0 || written.Any(number =>
                number.ValueKind != JsonValueKind.Number || !number.TryGetInt32(out var n) || n < 1))
            throw new InvalidDataException(MessageText.Format(EvalErrorMessage.BadExpectedNumber, new
            {
                scenario.Id,
                Key = key
            }));
        return [.. written.Select(number => number.GetInt32())];
    }

    /// <summary>
    /// What a file can set: the scenario key holding the number, the reader that reads it, and whether the check must
    /// pass.
    /// </summary>
    public sealed record Settings(string Key, string Reader, bool Required = true);

    /// <summary>The check as a file names it, <c>number-named</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("number-named", CheckKind.FixedRule)
    {
        /// <summary>
        /// Builds the check from its settings, through the number reader the file names; a blank key or reader is
        /// refused, as is a reader the catalogue doesn't have or one that reads no numbers.
        /// </summary>
        /// <remarks>Guard clauses, one blank setting each from the top (ADR 0051).</remarks>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            if (string.IsNullOrWhiteSpace(settings.Key)) throw Blank("key");
            if (string.IsNullOrWhiteSpace(settings.Reader)) throw Blank("reader");
            return new NumberNamedCheck(context.Readers.Get<INumberReader>(settings.Reader), settings.Key,
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
