using System.Text.Json;

namespace LouisAgent.Puls.Admin.Grades;

/// <summary>Reads a conversation row's <c>grades</c> and <c>outcome</c>; null where the conversation recorded none.</summary>
public static class ConversationGradesReader
{
    public static ConversationGrades Read(JsonElement conversation)
    {
        if (conversation.ValueKind != JsonValueKind.Object) return ConversationGrades.None;
        IReadOnlyList<GradeRow>? grades = conversation.TryGetProperty("grades", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(g => new GradeRow(Text(g, "grader") ?? "", Int(g, "version"), Score(g),
                GradeOutcomes.Parse(Text(g, "outcome")), Text(g, "reason") ?? "")).ToList()
            : null;
        GradeOutcome? outcome = Text(conversation, "outcome") is { } word ? GradeOutcomes.Parse(word) : null;
        return new ConversationGrades(grades, outcome);
    }

    /// <summary>A score that isn't a number reads as 0: an unknown score is never shown as partial credit.</summary>
    private static double Score(JsonElement grade) =>
        grade.TryGetProperty("score", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out double d) ? d : 0;

    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n) ? n : 0;
}
