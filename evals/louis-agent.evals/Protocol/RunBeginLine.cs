using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Protocol;

/// <summary>The fields of a run's first line (F18 step 2a): the eval, its version, the commit, its split, its graders and the model each mode ran on. The writer emits them.</summary>
internal static class RunBeginLine
{
    public static void Write(Utf8JsonWriter json, string eval, int evalVersion, string commit, IReadOnlyList<IGrader> graders, IReadOnlyList<ModelUse> models,
        SplitSelection split, string? rescoreOf = null, DatasetManifest? dataset = null)
    {
        json.WriteNumber("version", ProtocolWriter.Version);
        json.WriteString("eval", eval);
        json.WriteNumber("eval_version", evalVersion);
        json.WriteString("commit", commit);
        json.WriteString("split", SplitSelections.WireName(split));
        // F18 step 5b: the run this one re-scored, and the dataset it graded against as it was then (a run of an eval with no dataset file names none).
        if (rescoreOf is not null) json.WriteString("rescore_of", rescoreOf);
        if (dataset is not null)
        {
            json.WriteStartObject("dataset");
            json.WriteString("name", dataset.Name);
            json.WriteNumber("version", dataset.Version);
            json.WriteString("hash", dataset.ContentHash);
            json.WriteEndObject();
        }
        json.WriteStartArray("graders");
        foreach (var grader in graders)
        {
            json.WriteStartObject();
            json.WriteString("name", grader.Name);
            json.WriteNumber("version", grader.Version);
            json.WriteString("kind", grader.Kind.Wire());
            // A human review carries the question a person is asked (F18 step 6a.3), so the portal shows what that run was asked.
            if (grader is Grading.Human.HumanReviewGrader review) json.WriteString("question", review.Question);
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteStartArray("models");
        foreach (var use in models)
        {
            json.WriteStartObject();
            json.WriteString("mode", use.Mode);
            json.WriteString("level", use.Level.Wire());
            WriteText(json, "role", use.Role);
            WriteText(json, "model", use.Model);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    }

    private static void WriteText(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null) json.WriteNull(name);
        else json.WriteString(name, value);
    }
}
