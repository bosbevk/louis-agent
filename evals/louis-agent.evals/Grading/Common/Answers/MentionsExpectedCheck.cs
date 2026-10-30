using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Answers;

/// <summary>
/// The first turn's answer names the value the scenario expects under a key (the town, the shop, a number), with
/// case and accents ignored. A scenario without the key is refused, not passed, so a dataset mistake shows. A turn the
/// infrastructure broke is Unknown; a turn that streamed an error fails.
/// </summary>
/// <remarks>
/// A Specification over the answer (Evans and Fowler), fed from the scenario: the rule is plain text search, the
/// value is data, so one check serves every eval that states what an answer should name. It lists its key in
/// <see cref="ExpectedKeys"/>, so the dataset loader checks the key is read and fed, as for <c>routed-to</c>. Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class MentionsExpectedCheck(string key, bool required = true) : ICheck
{
    /// <summary>Ignore case and accents: a letter with a mark matches the same letter without one.</summary>
    private const CompareOptions Loosely = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <inheritdoc />
    public string Name => "mentions-expected";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { key };

    /// <summary>
    /// Looks for the scenario's value in the first turn's answer, after making sure that turn can be judged.
    /// </summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault(), ValueIn(trial.Scenario)));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error, then a pass or a
    /// fail on whether the answer names the value.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first, string value)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var answer = EngineEvents.AnswerOf(first);
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(answer, value, Loosely) >= 0
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.MentionsPass, new { Value = value }))
            : Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.MentionsMissing, new { Value = value }));
    }

    /// <summary>
    /// The value the scenario expects under the key, as text: a string as it is, a number as its JSON text. A
    /// scenario without the key, or with another kind of value, is refused, naming the scenario and the key.
    /// </summary>
    /// <remarks>A guard clause for the missing key, then a switch expression on the value's kind (ADR 0051).</remarks>
    private string ValueIn(EvalScenario scenario)
    {
        if (scenario.Expected.ValueKind != JsonValueKind.Object ||
            !scenario.Expected.TryGetProperty(key, out var value))
            throw Refuse(EvalErrorMessage.NoExpectedValue, scenario);
        return value.ValueKind switch
        {
            JsonValueKind.String when !string.IsNullOrWhiteSpace(value.GetString()) => value.GetString()!,
            JsonValueKind.Number => value.GetRawText(),
            _ => throw Refuse(EvalErrorMessage.BadExpectedValue, scenario)
        };
    }

    /// <summary>The refusal of a scenario's value, naming the scenario and the key.</summary>
    private InvalidDataException Refuse(EvalErrorMessage message, EvalScenario scenario) =>
        new(MessageText.Format(message, new
        {
            scenario.Id,
            Key = key
        }));

    /// <summary>
    /// What a file can set: the scenario key whose value the answer must name, and whether it must pass.
    /// </summary>
    public sealed record Settings(string Key, bool Required = true);

    /// <summary>The check as a file names it, <c>mentions-expected</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("mentions-expected", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a blank key: there would be nothing to read.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) =>
            !string.IsNullOrWhiteSpace(settings.Key)
                ? new MentionsExpectedCheck(settings.Key, settings.Required)
                : throw new InvalidDataException(MessageText.Format(CheckCatalogMessage.NoText, new
                {
                    Type,
                    Field = "key"
                }));
    }
}
