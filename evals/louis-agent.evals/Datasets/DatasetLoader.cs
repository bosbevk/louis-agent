using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Datasets;

/// <summary>
/// Reads a dataset folder (F18 step 3a) strictly: no JSON field it doesn't know, the split from the folder's name, every id
/// once, every expected key read by a grader, and the scenarios' hash equal to the manifest's. A folder it can't check is
/// refused with its text from <see cref="EvalErrorMessage"/>, as an <see cref="InvalidDataException"/>.
/// </summary>
public static class DatasetLoader
{
    private const string ManifestFileName = "dataset.json";
    private const string ScenariosFolder = "scenarios";

    /// <summary>The snake_case JSON of the eval's records, with a field the loader doesn't know refused.</summary>
    private static readonly JsonSerializerOptions Strict = new(EvalScores.Json) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    /// <summary>The folder names a split can have, parsed once where they enter (ADR 0024).</summary>
    private static readonly IReadOnlyDictionary<string, DatasetSplit> SplitAliases = new Dictionary<string, DatasetSplit> { ["held-out"] = DatasetSplit.HeldOut };

    public static Dataset Load(string folder, IReadOnlyList<IGrader> graders)
    {
        ManifestFile file = Read<ManifestFile>(Path.Combine(folder, ManifestFileName), EvalErrorMessage.InvalidManifest);
        var manifest = new DatasetManifest(file.Name, file.Version, file.Covers, file.ContentHash, file.Changes);
        string scenarios = Path.Combine(folder, ScenariosFolder);

        string hash = DatasetHash.Of(scenarios);
        if (!string.Equals(hash, manifest.ContentHash, StringComparison.OrdinalIgnoreCase))
            throw Refuse(EvalErrorMessage.HashChanged, new { Dataset = manifest.Name, Hash = hash });

        IReadOnlySet<string> readKeys = graders.SelectMany(grader => grader.ExpectedKeys).ToHashSet();
        List<EvalScenario> found = ReadScenarios(scenarios, readKeys);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (EvalScenario scenario in found)
            if (!ids.Add(scenario.Id))
                throw Refuse(EvalErrorMessage.DuplicateScenarioId, new { Dataset = manifest.Name, Id = scenario.Id });

        return new Dataset(manifest, found);
    }

    /// <summary>Every scenario, dev first, then held-out; each folder's files in name order.</summary>
    private static List<EvalScenario> ReadScenarios(string scenarios, IReadOnlySet<string> readKeys)
    {
        var found = new List<EvalScenario>();
        if (!Directory.Exists(scenarios)) return found;

        foreach (string directory in Directory.GetDirectories(scenarios))
        {
            string name = Path.GetFileName(directory);
            if (!SettingChoice.TryParse(name, out DatasetSplit? split, SplitAliases) || split is null)
                throw Refuse(EvalErrorMessage.UnknownSplitFolder, new { Folder = name });

            foreach (string file in Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal))
                found.Add(ReadScenario(file, split.Value, readKeys));
        }
        return found.OrderBy(scenario => scenario.Split).ToList();
    }

    private static EvalScenario ReadScenario(string file, DatasetSplit split, IReadOnlySet<string> readKeys)
    {
        ScenarioFile scenario = Read<ScenarioFile>(file, EvalErrorMessage.InvalidScenario);
        if (scenario.Expected.ValueKind != JsonValueKind.Object)
            throw Refuse(EvalErrorMessage.ExpectedNotAnObject, new { Id = scenario.Id });

        foreach (JsonProperty criterion in scenario.Expected.EnumerateObject())
            if (!readKeys.Contains(criterion.Name))
                throw Refuse(EvalErrorMessage.UnreadExpectedKey, new { Id = scenario.Id, Key = criterion.Name });

        return new EvalScenario(scenario.Id, scenario.Input, scenario.Expected, scenario.Categories, split);
    }

    /// <summary>A JSON file read strictly; a file that doesn't parse, or holds no object, is refused with the given text.</summary>
    private static T Read<T>(string file, EvalErrorMessage refusal) where T : class
    {
        string name = Path.GetFileName(file);
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(file), Strict)
                ?? throw Refuse(refusal, new { File = name, Reason = MessageText.Format(EvalErrorMessage.EmptyFile) });
        }
        catch (JsonException error)
        {
            throw Refuse(refusal, new { File = name, Reason = error.Message });
        }
    }

    private static InvalidDataException Refuse(EvalErrorMessage message, object values) => new(MessageText.Format(message, values));

    /// <summary>A manifest as <c>dataset.json</c> holds it: every field is required.</summary>
    private sealed class ManifestFile
    {
        public required string Name { get; init; }
        public required int Version { get; init; }
        public required string Covers { get; init; }
        public required string ContentHash { get; init; }
        public required IReadOnlyList<string> Changes { get; init; }
    }

    /// <summary>One scenario file: the input, the expected criteria as the graders read them, and the categories.</summary>
    private sealed class ScenarioFile
    {
        public required string Id { get; init; }
        public required EvalInput Input { get; init; }
        public required JsonElement Expected { get; init; }
        public IReadOnlyList<string> Categories { get; init; } = [];
    }
}
