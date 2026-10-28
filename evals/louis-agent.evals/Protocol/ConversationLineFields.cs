using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The fields of a <c>conversation</c> line (F18 step 4.5a): the eval's scores, every grade with who judged it, and how
/// the trial came out. The writer wraps them in the line's event and time.
/// </summary>
/// <remarks>
/// A field writer per kind of line, the way <see cref="RunBeginLine"/> and <see cref="SummaryLineFields"/> already work
/// in this folder. The conversation line is the longest one, with its list of grades, so it gets its own class and the
/// writer keeps only the flow. Without it the writer passes two hundred lines and one class holds every line of the
/// protocol.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0010-folders-by-concern.md">ADR 0010: folders by concern</seealso>
internal static class ConversationLineFields
{
    /// <summary>
    /// Writes the conversation's fields in the order the Python harness wrote them. A field that is absent is left out.
    /// </summary>
    public static void Write(Utf8JsonWriter json, string mode, int repeat, string task, string? role,
        JsonElement scores,
        double seconds, IReadOnlyList<Grade>? grades, TrialOutcome? outcome, int? errors)
    {
        json.WriteString("mode", mode);
        json.WriteNumber("repeat", repeat);
        json.WriteString("task", task);
        ProtocolWriter.WriteText(json, "role", role);
        json.WritePropertyName("scores");
        scores.WriteTo(json);
        if (errors is not null) json.WriteNumber("errors", errors.Value);
        json.WriteNumber("seconds", seconds);
        if (grades is not null) WriteGrades(json, grades);
        if (outcome is not null) json.WriteString("outcome", V1Words.Trial(outcome.Value));
    }

    /// <summary>
    /// The grades, each with its grader, score and reason, and the judge's model and prompt when a model graded it.
    /// </summary>
    private static void WriteGrades(Utf8JsonWriter json, IReadOnlyList<Grade> grades)
    {
        json.WriteStartArray("grades");
        foreach (var grade in grades)
        {
            json.WriteStartObject();
            json.WriteString("grader", grade.Grader);
            json.WriteNumber("version", grade.Version);
            json.WriteNumber("score", grade.Score);
            json.WriteString("outcome", V1Words.Outcome(grade.Outcome));
            json.WriteString("reason", grade.Reason);
            if (grade.Judge is { } judge)
            {
                json.WriteString("judge_model", judge.Model);
                json.WriteString("judge_prompt_hash", judge.PromptHash);
            }

            json.WriteEndObject();
        }

        json.WriteEndArray();
    }
}
