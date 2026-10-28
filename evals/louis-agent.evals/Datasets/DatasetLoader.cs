using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Choices;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;

namespace LouisAgent.Evals.Datasets;

/// <summary>
/// Reads a dataset folder (F18 step 3a) strictly: no JSON field it doesn't know, no missing required one, the split from the
/// folder's name, every scenario file where the hash sees it, every id once, every expected key read by a grader and every key a grader reads fed by a scenario, one dated
/// change line per version, and the scenarios' hash equal to the manifest's. A folder it can't check is refused with its text
/// from <see cref="EvalErrorMessage"/>, as an <see cref="InvalidDataException"/>.
/// </summary>
public static class DatasetLoader
{
    private const string ManifestFileName = "dataset.json";
    private const string ScenariosFolder = "scenarios";

    /// <summary>
    /// The eval's JSON, strict: a field the loader doesn't know is refused, and so is a missing constructor parameter or a null
    /// where the type isn't nullable. Without the last two, a missing <c>time_zone</c> would load as null and fail later.
    /// </summary>
    private static readonly JsonSerializerOptions Strict = new(EvalJson.SnakeCase)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
    };

    /// <summary>The folder names a split can have, parsed once where they enter (ADR 0024).</summary>
    private static readonly IReadOnlyDictionary<string, DatasetSplit> SplitAliases =
        new Dictionary<string, DatasetSplit> { ["held-out"] = DatasetSplit.HeldOut };

    public static Dataset Load(string folder, IReadOnlyList<IGrader> graders)
    {
        string manifestPath = Path.Combine(folder, ManifestFileName);
        if (!File.Exists(manifestPath)) throw Refuse(EvalErrorMessage.NoManifest, new { Folder = folder });

        ManifestFile file = Read<ManifestFile>(manifestPath, EvalErrorMessage.InvalidManifest);
        var manifest = new DatasetManifest(file.Name, file.Version, file.Covers, file.ContentHash, file.Changes);
        if (manifest.Version < 1 || manifest.Changes.Count != manifest.Version)
            throw Refuse(EvalErrorMessage.ChangesNotVersion,
                new { Dataset = manifest.Name, Version = manifest.Version, Count = manifest.Changes.Count });

        // The files are read before the hash is compared: a stray file is then named as the problem, not as a change to bump.
        string scenarios = Path.Combine(folder, ScenariosFolder);
        IReadOnlySet<string> readKeys = graders.SelectMany(grader => grader.ExpectedKeys).ToHashSet();
        List<EvalScenario> found = ReadScenarios(scenarios, readKeys);

        string hash = DatasetHash.Of(scenarios);
        if (!string.Equals(hash, manifest.ContentHash, StringComparison.OrdinalIgnoreCase))
            throw Refuse(EvalErrorMessage.HashChanged,
                new { Dataset = manifest.Name, Hash = hash, Recorded = manifest.ContentHash });

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (EvalScenario scenario in found)
            if (!ids.Add(scenario.Id))
                throw Refuse(EvalErrorMessage.DuplicateScenarioId, new { Dataset = manifest.Name, Id = scenario.Id });

        // A grader that reads a key no scenario sets would grade nothing, and say so nowhere (F18 step 5.4).
        foreach (string key in readKeys.Order(StringComparer.Ordinal))
            if (!found.Any(scenario => scenario.Expected.TryGetProperty(key, out _)))
                throw Refuse(EvalErrorMessage.UnfedKey, new { Dataset = manifest.Name, Key = key });

        return new Dataset(manifest, found,
            Labels.LabelLoader.Load(folder, found, graders.Select(grader => grader.Name).ToHashSet()));
    }

    /// <summary>
    /// Every scenario, dev first, then held-out. It reads the same files the hash covers, so a file is loaded or refused, never
    /// skipped.
    /// </summary>
    private static List<EvalScenario> ReadScenarios(string scenarios, IReadOnlySet<string> readKeys) =>
        DatasetHash.Files(scenarios)
            .Select(file => ReadScenario(file.Relative, file.Full, readKeys))
            .OrderBy(scenario => scenario.Split)
            .ToList();

    /// <summary>A scenario file must be <c>{dev|held-out}/{name}.json</c>, with the name in lowercase and the extension ordinal.</summary>
    private static EvalScenario ReadScenario(string relative, string full, IReadOnlySet<string> readKeys)
    {
        string[] parts = relative.Split('/');
        if (parts is [var folder, _] && !TryParseSplit(folder, out _))
            throw Refuse(EvalErrorMessage.UnknownSplitFolder, new { Folder = folder });
        if (parts is not [var split, var name] || !TryParseSplit(split, out DatasetSplit parsed) ||
            !IsScenarioName(name))
            throw Refuse(EvalErrorMessage.UnexpectedFile, new { File = relative });

        ScenarioFile scenario = Read<ScenarioFile>(full, EvalErrorMessage.InvalidScenario);
        if (scenario.Input.Messages.Count == 0) throw Refuse(EvalErrorMessage.NoMessages, new { Id = scenario.Id });
        if (scenario.Expected.ValueKind != JsonValueKind.Object)
            throw Refuse(EvalErrorMessage.ExpectedNotAnObject, new { Id = scenario.Id });

        foreach (JsonProperty criterion in scenario.Expected.EnumerateObject())
            if (!readKeys.Contains(criterion.Name))
                throw Refuse(EvalErrorMessage.UnreadExpectedKey, new { Id = scenario.Id, Key = criterion.Name });

        return new EvalScenario(scenario.Id, scenario.Input, scenario.Expected, scenario.Categories, parsed);
    }

    private static bool TryParseSplit(string folder, out DatasetSplit split)
    {
        split = default;
        if (!SettingChoice.TryParse(folder, out DatasetSplit? parsed, SplitAliases) || parsed is null) return false;
        split = parsed.Value;
        return true;
    }

    private static bool IsScenarioName(string name) =>
        name.EndsWith(".json", StringComparison.Ordinal) && name == name.ToLowerInvariant();

    /// <summary>A JSON file read strictly; a file that doesn't parse, or holds no object, is refused with the given text.</summary>
    private static T Read<T>(string file, EvalErrorMessage refusal) where T : class
    {
        string name = Path.GetFileName(file);
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(file), Strict)
                   ?? throw Refuse(refusal,
                       new { File = name, Reason = MessageText.Format(EvalErrorMessage.EmptyFile) });
        }
        catch (JsonException error)
        {
            throw Refuse(refusal, new { File = name, Reason = error.Message });
        }
    }

    private static InvalidDataException Refuse(EvalErrorMessage message, object values) =>
        new(MessageText.Format(message, values));

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
