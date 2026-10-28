using LouisAgent.Choices;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Parts.Run;
using ModelLevel = LouisAgent.Evals.Protocol.Kinds.ModelLevel;
using RunSplit = LouisAgent.Evals.Protocol.Kinds.RunSplit;

namespace LouisAgent.Evals.Protocol;

/// <summary>
/// The fields of a run's first line: the eval, its version, the commit, its split, its graders and the model each mode
/// ran on. The writer emits them.
/// </summary>
internal static class RunBeginLine
{
    /// <summary>Writes the first line's fields, after the event's name and time.</summary>
    public static void Write(Utf8JsonWriter json, string eval, int evalVersion, string commit,
        IReadOnlyList<IGrader> graders, IReadOnlyList<ModelInfo> models,
        RunSplit split, string? rescoreOf = null, DatasetManifest? dataset = null)
    {
        json.WriteNumber("version", ProtocolWriter.Version);
        json.WriteString("eval", eval);
        json.WriteNumber("eval_version", evalVersion);
        json.WriteString("commit", commit);
        json.WriteString("split", V1Split(split));
        // The run this one re-scored, and the dataset it graded against as it was then (an eval with no dataset file
        // names none).
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
            // A human review carries the question a person is asked, so the portal shows what that run asked.
            if (grader is Grading.Human.HumanReviewGrader review) json.WriteString("question", review.Question);
            json.WriteEndObject();
        }

        json.WriteEndArray();
        json.WriteStartArray("models");
        foreach (var use in models)
        {
            json.WriteStartObject();
            json.WriteString("mode", use.Mode);
            json.WriteString("level", V1Level(use.Level));
            WriteText(json, "role", use.Role);
            WriteText(json, "model", use.Model);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    /// <summary>
    /// The split as version 1 writes it, with an underscore (<c>held_out</c>), until step 7c.2c writes the contract's
    /// own words.
    /// </summary>
    /// <remarks>A switch expression on the split, one arm per case, read like a table.</remarks>
    private static string V1Split(RunSplit split) => split switch
    {
        RunSplit.HeldOut => "held_out",
        _ => split.Wire(),
    };

    /// <summary>
    /// The level as version 1 writes it, its member's name (<c>Agent</c>), until step 7c.2c writes the contract's own
    /// words.
    /// </summary>
    /// <remarks>A switch expression on the level, one arm per case, read like a table.</remarks>
    private static string V1Level(ModelLevel level) => level switch
    {
        ModelLevel.Agent => "Agent",
        ModelLevel.Specialist => "Specialist",
        ModelLevel.Minion => "Minion",
        ModelLevel.Grader => "Grader",
        _ => throw new ArgumentOutOfRangeException(nameof(level)),
    };

    /// <summary>A text field, written as <c>null</c> when there is none, so the line always has the field.</summary>
    private static void WriteText(Utf8JsonWriter json, string name, string? value)
    {
        if (value is null) json.WriteNull(name);
        else json.WriteString(name, value);
    }
}
