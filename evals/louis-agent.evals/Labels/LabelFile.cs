using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Labels;

/// <summary>A person's verdict on one answer (F18 step 6.3): a meal, or not, with a note.</summary>
public sealed record AnswerLabel([property: JsonRequired] bool? Meal, string Note = "");

/// <summary>
/// One label file, <c>labels/{scenario}.{mode}.{run}.{repeat}.json</c>: which answer it labels (the scenario, the mode and repeat, the run it came from, the turn whose answer it
/// is, and the answer's text, so the label survives the run being deleted) and the person's <see cref="Label"/>, null until they give it.
/// </summary>
public sealed record LabelFile(string Scenario, string Mode, string Run, int Repeat, int Turn, string Answer, AnswerLabel? Label);

/// <summary>A labelled answer as the dataset holds it: the file it came from, what it labels and the person's verdict.</summary>
public sealed record LabeledAnswer(string File, string Scenario, string Mode, string Run, int Repeat, int Turn, string Answer, bool Meal, string Note);

/// <summary>The one JSON wire of the label files (ADR 0041): snake_case, strict, so a typo in a file is refused rather than ignored.</summary>
public static class LabelJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
