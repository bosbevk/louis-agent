using System.Text.Json;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Evals.Drafts.Composed;

/// <summary>
/// Makes an eval draft from what the compose page sent: the file must be a JSON object that names its eval and the
/// identity it proves, both in kebab-case, as the folder they become is named; each rubric is a kebab-case markdown
/// file; someone must have made it; and nothing is so big that one request fills the store.
/// </summary>
/// <remarks>
/// A static factory (ADR 0040's <c>Of</c>) with guard clauses (ADR 0051), one per refusal, in the order the page shows
/// the fields. It reads only the two names it files the draft under; the rest of the file is the runner's to judge
/// (step 3's <c>--check</c>), so a draft can be saved half-made. A refusal's message is what the person reads.
/// </remarks>
public static class ComposedEvals
{
    /// <summary>The largest file and rubric a draft takes, and how many rubrics.</summary>
    public const int MaxJsonLength = 64 * 1024, MaxRubricLength = 32 * 1024, MaxRubrics = 10;

    /// <summary>The draft for this file and these rubrics, saved by this person now.</summary>
    public static EvalDraft Of(string id, string? json, IReadOnlyList<RubricFile>? rubrics, string? author,
        DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(author)) throw Refuse(DraftMessage.AuthorBlank, null);
        if (json is null || json.Length > MaxJsonLength)
            throw Refuse(DraftMessage.EvalFileTooBig, new { Limit = MaxJsonLength });
        var (name, identity) = NamesOf(json);
        var kept = rubrics ?? [];
        if (kept.Count > MaxRubrics) throw Refuse(DraftMessage.TooManyRubrics, new { Limit = MaxRubrics });
        if (kept.FirstOrDefault(r => !IsRubricName(r.Name)) is { } badName)
            throw Refuse(DraftMessage.RubricNameNotKebabCase, new { badName.Name });
        if (kept.FirstOrDefault(r => r.Text.Length > MaxRubricLength) is { } tooLong)
            throw Refuse(DraftMessage.RubricTooBig, new
            {
                tooLong.Name,
                Limit = MaxRubricLength
            });

        return new EvalDraft(id, identity, name, json, kept, author.Trim(), at);
    }

    /// <summary>The eval's name and identity, from its file, each in kebab-case.</summary>
    /// <remarks>Guard clauses: not JSON or not an object, then each name missing or not kebab-case.</remarks>
    private static (string Name, string Identity) NamesOf(string json)
    {
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(json).RootElement.Clone();
        }
        catch (JsonException)
        {
            throw Refuse(DraftMessage.EvalFileNotAnObject, null);
        }

        if (root.ValueKind != JsonValueKind.Object) throw Refuse(DraftMessage.EvalFileNotAnObject, null);
        string? name = Text(root, "name"), identity = Text(root, "identity");
        if (!KebabCase.Is(name)) throw Refuse(DraftMessage.EvalFileNeedsAName, new { Field = "name" });
        if (!KebabCase.Is(identity)) throw Refuse(DraftMessage.EvalFileNeedsAName, new { Field = "identity" });
        return (name!, identity!);
    }

    /// <summary>A field's text, when the file has it as text.</summary>
    private static string? Text(JsonElement root, string field) =>
        root.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Whether a rubric's file name is a kebab-case name with <c>.md</c> after it.</summary>
    private static bool IsRubricName(string name) =>
        name.EndsWith(".md", StringComparison.Ordinal) && KebabCase.Is(name[..^3]);

    /// <summary>The refusal the person reads.</summary>
    private static ArgumentException Refuse(DraftMessage message, object? values) =>
        new(MessageText.Format(message, values));
}
