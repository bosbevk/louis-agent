using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Protocol;

/// <summary>The fields of a run's first line (F18 step 2a): the eval, its version, the commit, its split, its graders and the model each mode ran on. The writer emits them.</summary>
internal static class RunBeginLine
{
    public static void Write(Utf8JsonWriter json, string eval, int evalVersion, string commit, IReadOnlyList<IGrader> graders, IReadOnlyList<ModelUse> models,
        SplitSelection split)
    {
        json.WriteNumber("version", ProtocolWriter.Version);
        json.WriteString("eval", eval);
        json.WriteNumber("eval_version", evalVersion);
        json.WriteString("commit", commit);
        json.WriteString("split", SplitSelections.WireName(split));
        json.WriteStartArray("graders");
        foreach (var grader in graders)
        {
            json.WriteStartObject();
            json.WriteString("name", grader.Name);
            json.WriteNumber("version", grader.Version);
            json.WriteString("kind", grader.Kind.ToString());
            json.WriteEndObject();
        }
        json.WriteEndArray();
        json.WriteStartArray("models");
        foreach (var use in models)
        {
            json.WriteStartObject();
            json.WriteString("mode", use.Mode);
            json.WriteString("level", use.Level.ToString());
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
