using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Answers;

/// <summary>
/// The first turn's answer is JSON: either the whole answer, trimmed, or the inside of its one fenced block (a line
/// that starts with three backticks, to the next such line; <c>json</c> after the backticks is allowed). A fail
/// quotes what the JSON reader said; an answer with more than one block fails and says so, rather than guessing which
/// one is meant. A turn the infrastructure broke is Unknown; a turn that streamed an error fails.
/// </summary>
/// <remarks>
/// A structure check: it asks whether the answer has a shape a program can read, not what it says. The parse is .NET's
/// own JSON reader, and the fences are found line by line with plain string methods (ADR 0057). Its
/// <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed class IsJsonCheck(bool required = true) : ICheck
{
    /// <summary>What a fence line starts with.</summary>
    private const string Fence = "```";

    /// <inheritdoc />
    public string Name => "is-json";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Reads the first turn's answer as JSON, after making sure that turn can be judged.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GradeFirst(trial.Transcript.Turns.FirstOrDefault()));

    /// <summary>
    /// The grade of the first turn: Unknown with no turn or a broken one, a fail on a streamed error or on fences that
    /// don't make one block, then a pass or a fail on whether the JSON reader accepts the text.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade GradeFirst(TurnTranscript? first)
    {
        if (first is null) return Verdict.Unknown(Name, Version, MessageText.Format(CommonGradeMessage.NoTurn));
        if (Verdict.InfrastructureFailure(Name, Version, [first]) is { } unknown) return unknown;
        if (EngineEvents.StreamedError(first))
            return Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.TurnFailed));

        var lines = EngineEvents.AnswerOf(first).Split('\n');
        var fences = lines.Select((line, index) => (line, index))
            .Where(pair => pair.line.TrimStart().StartsWith(Fence, StringComparison.Ordinal))
            .Select(pair => pair.index).ToList();
        if (fences.Count is not (0 or 2))
            return Verdict.Fail(Name, Version,
                MessageText.Format(CommonGradeMessage.FencesUnclear, new { fences.Count }));

        // No fence: the whole answer; one block: the lines between its two fences.
        var text = fences.Count == 0
            ? string.Join('\n', lines)
            : string.Join('\n', lines[(fences[0] + 1)..fences[1]]);
        return Parses(text.Trim(), out var reason)
            ? Verdict.Pass(Name, Version, MessageText.Format(CommonGradeMessage.IsJsonPass))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.NotJson, new { Reason = reason }));
    }

    /// <summary>Whether the JSON reader accepts the text; when it doesn't, what it said.</summary>
    private static bool Parses(string text, out string reason)
    {
        try
        {
            using var _ = JsonDocument.Parse(text);
            reason = "";
            return true;
        }
        catch (JsonException error)
        {
            reason = error.Message;
            return false;
        }
    }

    /// <summary>What a file can set: whether the check must pass.</summary>
    public sealed record Settings(bool Required = true);

    /// <summary>The check as a file names it, <c>is-json</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("is-json", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings.</summary>
        protected override ICheck Create(Settings settings, CheckContext context) => new IsJsonCheck(settings.Required);
    }
}
