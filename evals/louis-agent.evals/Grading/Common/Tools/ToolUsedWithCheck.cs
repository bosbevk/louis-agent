using System.Globalization;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Grading.Common.Tools;

/// <summary>
/// Someone in the trial, the agent or a specialist, called the tool with one input field holding the value: the same
/// text, case and accents ignored, a number or true/false read as its JSON text. "The shop was searched at Checkers"
/// is <c>FindProducts</c> with <c>shop</c> <c>checkers</c>. A value inside a longer text isn't the same text.
/// </summary>
/// <remarks>
/// A Specification over one tool input, reading the calls as <c>tool-used</c> does. Same text and not "contains"
/// (Louis's pick): a check of a shop or a mode should not match a longer name. With no matching call found, a broken
/// turn makes it Unknown. Its <see cref="Entry"/> builds it from a file's settings (Factory Method).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class ToolUsedWithCheck(string tool, string field, string value, bool required = true) : ICheck
{
    /// <summary>Ignore case and accents: a letter with a mark matches the same letter without one.</summary>
    private const CompareOptions Loosely = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    /// <inheritdoc />
    public string Name => "tool-used-with";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public CheckKind Kind => CheckKind.FixedRule;

    /// <inheritdoc />
    public bool Required => required;

    /// <inheritdoc />
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();

    /// <summary>Looks through every turn's calls of the tool for one whose field holds the value.</summary>
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Look(trial.Transcript.Turns));

    /// <summary>
    /// A pass naming the first turn with a matching call; Unknown when there was none and a turn broke; otherwise a
    /// fail that says whether the tool was called at all.
    /// </summary>
    /// <remarks>Guard clauses, one case each from the top (ADR 0051).</remarks>
    private Grade Look(IReadOnlyList<TurnTranscript> turns)
    {
        if (turns.FirstOrDefault(turn => EngineEvents.ToolInputsOf(turn, tool).Any(Holds)) is { } match)
            return Verdict.Pass(Name, Version,
                MessageText.Format(CommonGradeMessage.ToolUsedWithPass, new
                {
                    Tool = tool,
                    Field = field,
                    Value = value,
                    match.Turn
                }));
        if (Verdict.InfrastructureFailure(Name, Version, turns) is { } unknown) return unknown;
        return turns.Any(turn => EngineEvents.ToolInputsOf(turn, tool).Count > 0)
            ? Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ToolCalledOtherwise, new
            {
                Tool = tool,
                Field = field,
                Value = value
            }))
            : Verdict.Fail(Name, Version, MessageText.Format(CommonGradeMessage.ToolNotUsed, new { Tool = tool }));
    }

    /// <summary>Whether this call's input holds the value in the field, as text, case and accents ignored.</summary>
    private bool Holds(JsonElement input) =>
        TextOf(input, field) is { } text &&
        CultureInfo.InvariantCulture.CompareInfo.Compare(text, value, Loosely) == 0;

    /// <summary>
    /// The field's value as text: a string as it is, a number or true/false as its JSON text; null for anything else.
    /// </summary>
    /// <remarks>A guard clause for a missing field, then a switch expression on the value's kind (ADR 0051).</remarks>
    private static string? TextOf(JsonElement input, string name)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty(name, out var found)) return null;
        return found.ValueKind switch
        {
            JsonValueKind.String => found.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => found.GetRawText(),
            _ => null
        };
    }

    /// <summary>
    /// What a file can set: the tool, the input field and its value, and whether the check must pass.
    /// </summary>
    public sealed record Settings(string Tool, string Field, string Value, bool Required = true);

    /// <summary>The check as a file names it, <c>tool-used-with</c>.</summary>
    public sealed class Entry() : CheckType<Settings>("tool-used-with", CheckKind.FixedRule)
    {
        /// <summary>Builds the check from its settings, refusing a blank tool, field or value, naming which.</summary>
        /// <remarks>Guard clauses, one blank setting each from the top (ADR 0051).</remarks>
        protected override ICheck Create(Settings settings, CheckContext context)
        {
            if (string.IsNullOrWhiteSpace(settings.Tool)) throw Refused(CheckCatalogMessage.NoText, "tool");
            if (string.IsNullOrWhiteSpace(settings.Field)) throw Refused(CheckCatalogMessage.NoText, "field");
            if (string.IsNullOrWhiteSpace(settings.Value)) throw Refused(CheckCatalogMessage.NoText, "value");
            return new ToolUsedWithCheck(settings.Tool, settings.Field, settings.Value, settings.Required);
        }
    }
}
