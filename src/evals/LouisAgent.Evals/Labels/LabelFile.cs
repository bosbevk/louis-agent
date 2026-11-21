using System.Text.Json;
using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Labels;

/// <summary>A person's verdict on one answer (F18 step 6.3): whether the grader should pass it, with a note; the same two words as a grade, so a human grade exports to it (6a).</summary>
public sealed record AnswerLabel([property: JsonRequired] bool? Pass, string Note = "");

/// <summary>One turn of a labelled conversation (6a.4f): what was asked and what was answered. The last turn's answer is the one labelled; the earlier ones are what it may rely on.</summary>
public sealed record LabelTurn(string Question, string Answer);

/// <summary>
/// One label file, <c>labels/{grader}.{scenario}.{mode}.{run}.{repeat}.json</c>: the grader it labels for, which conversation it labels (the scenario, the mode and repeat, the run it came
/// from) and that conversation's <see cref="Turns"/>, every question and answer so the label survives the run being deleted and a judge can see the whole conversation (6a.4f, Louis: a
/// follow-up turn's answer relies on the recipe given earlier), and the person's <see cref="Label"/> of the last turn's answer, null until they give it. Nothing in it names a product, so
/// the next judge uses the same shape.
/// </summary>
public sealed record LabelFile(
    string Grader,
    string Scenario,
    string Mode,
    string Run,
    int Repeat,
    IReadOnlyList<LabelTurn> Turns,
    AnswerLabel? Label)
{
    /// <summary>The turn whose answer is labelled: the last one.</summary>
    [JsonIgnore]
    public int Turn => Turns.Count;

    /// <summary>The answer that is labelled: the last turn's.</summary>
    [JsonIgnore]
    public string Answer => Turns.Count == 0 ? "" : Turns[^1].Answer;
}

/// <summary>A labelled answer as the dataset holds it: the file it came from, the conversation it labels and the person's verdict on its last answer.</summary>
public sealed record LabeledAnswer(
    string File,
    string Grader,
    string Scenario,
    string Mode,
    string Run,
    int Repeat,
    IReadOnlyList<LabelTurn> Turns,
    bool Pass,
    string Note)
{
    public int Turn => Turns.Count;

    public string Answer => Turns.Count == 0 ? "" : Turns[^1].Answer;
}

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
