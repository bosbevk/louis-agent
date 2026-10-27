using System.Text.Json;

namespace LouisAgent.Puls.Admin.Grades.Reading;

/// <summary>
/// Reads a conversation row's <c>grades</c> and <c>outcome</c>; null where the conversation recorded none, and a grade's score or
/// version null where it is missing or isn't a number, never 0 (ADR 0007).
/// </summary>
public static class ConversationGradesReader
{
    public static ConversationGrades Read(JsonElement conversation)
    {
        if (conversation.ValueKind != JsonValueKind.Object) return ConversationGrades.None;
        IReadOnlyList<GradeRow>? grades =
            conversation.TryGetProperty("grades", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().Select(g => new GradeRow(Text(g, "grader") ?? "", Int(g, "version"), Score(g),
                    GradeOutcomes.Parse(Text(g, "outcome")), Text(g, "reason") ?? "")).ToList()
                : null;
        GradeOutcome? outcome = Text(conversation, "outcome") is { } word ? GradeOutcomes.Parse(word) : null;
        return new ConversationGrades(grades, outcome, Human(conversation));
    }

    /// <summary>The people's grades (step 6a.3); null where the field is missing, so an older API reads as no grades.</summary>
    private static IReadOnlyList<HumanGradeRow>? Human(JsonElement conversation) =>
        conversation.TryGetProperty("human_grades", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(h => new HumanGradeRow(Text(h, "grader") ?? "",
                Text(h, "model_grader") ?? "", Text(h, "verdict") ?? "", Text(h, "note") ?? "",
                Text(h, "reviewer") ?? "",
                Text(h, "at") is { } at && DateTimeOffset.TryParse(at, out var when) ? when : default)).ToList()
            : null;

    private static double? Score(JsonElement grade) =>
        grade.TryGetProperty("score", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out double d)
            ? d
            : null;

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) &&
                                                           v.ValueKind == JsonValueKind.Number &&
                                                           v.TryGetInt32(out int n)
        ? n
        : null;
}
