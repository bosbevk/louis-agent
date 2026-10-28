using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Evals.Reports;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The fields of a <c>summary</c> line: one slice of a mode's roll-up. Written by hand like the protocol's other
/// lines, not by the serializer, because the writer owns two things a serializer's defaults would get wrong: the
/// ASCII-only escaping every line has, and a rate that is absent (never null, never 0) when the slice has too few
/// trials. The names are snake_case as the other lines' are. The cost is not here: the ledger prices a slice, and the
/// API adds it (ADR 0007).
/// </summary>
internal static class SummaryLineFields
{
    /// <summary>Writes one slice's fields, after the event's name and time.</summary>
    public static void Write(Utf8JsonWriter json, SummaryLine line)
    {
        json.WriteString("mode", line.Mode);
        json.WriteString("slice", line.Slice);
        json.WriteNumber("n", line.N);
        json.WriteNumber("passed", line.Passed);
        json.WriteNumber("failed", line.Failed);
        json.WriteNumber("unknown", line.Unknown);
        json.WriteNumber("infrastructure", line.Infrastructure);
        json.WriteStartArray("tasks");
        foreach (string task in line.Tasks) json.WriteStringValue(task);
        json.WriteEndArray();
        json.WriteBoolean("too_few", line.TooFew);
        if (line.PassRate is { } passRate) json.WriteNumber("pass_rate", passRate);
        if (line.PassAtK is { } passAtK) json.WriteNumber("pass_at_k", passAtK);
        if (line.PassHatK is { } passHatK) json.WriteNumber("pass_hat_k", passHatK);

        json.WriteStartArray("graders");
        foreach (var grader in line.Graders)
        {
            json.WriteStartObject();
            json.WriteString("grader", grader.Grader);
            json.WriteString("kind", V1Words.Kind(grader.Kind));
            Counts(json, grader.Passed, grader.Failed, grader.Unknown);
            json.WriteEndObject();
        }

        json.WriteEndArray();

        json.WriteStartArray("kinds");
        foreach (var kind in line.Kinds)
        {
            json.WriteStartObject();
            json.WriteString("kind", V1Words.Kind(kind.Kind));
            Counts(json, kind.Passed, kind.Failed, kind.Unknown);
            json.WriteEndObject();
        }

        json.WriteEndArray();

        json.WriteStartArray("scores");
        foreach (var score in line.Scores)
        {
            json.WriteStartObject();
            json.WriteString("key", score.Key);
            json.WriteNumber("mean", score.Mean);
            json.WriteNumber("n", score.N);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    /// <summary>How many passed, failed and couldn't tell.</summary>
    private static void Counts(Utf8JsonWriter json, int passed, int failed, int unknown)
    {
        json.WriteNumber("passed", passed);
        json.WriteNumber("failed", failed);
        json.WriteNumber("unknown", unknown);
    }
}
