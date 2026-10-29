using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Samples;
using LouisAgent.Evals.Runner.Messages;

namespace LouisAgent.Evals.Runner.Samples;

/// <summary>
/// The one-off <c>convert-samples {folder}</c> command (F18 step 7d): rewrites a dataset's scenario files from the old
/// shape (an input object with every message, criteria at the top) into Inspect's Sample shape, then moves its
/// manifest one version on with a dated line and the new hash. Run once per dataset, then deleted with this step.
/// </summary>
/// <remarks>
/// A command, not a script over the files (the brief: never a script that rewrites them): the new file is written
/// through the same <see cref="SampleFile"/> records the loader reads, so the two shapes can't drift.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class SampleConversion
{
    /// <summary>How the old files are read: snake_case, every field the old shape had.</summary>
    private static readonly JsonSerializerOptions Read = new(EvalJson.SnakeCase);

    /// <summary>
    /// How the new files are written: snake_case, indented as the datasets are, one newline on any machine, and
    /// letters such as ê kept as they are (they are data people read, not markup).
    /// </summary>
    private static readonly JsonSerializerOptions Write = new(EvalJson.SnakeCase)
    {
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Converts every scenario of the dataset in this folder, moves its manifest on, and says how many.
    /// </summary>
    public static int Convert(string folder, DateOnly today)
    {
        string scenarios = Path.Combine(folder, "scenarios");
        var files = Directory.EnumerateFiles(scenarios, "*.json", SearchOption.AllDirectories).ToList();
        foreach (string file in files)
            File.WriteAllText(file, Sample(File.ReadAllText(file)));

        // The manifest last, so its hash is the converted files'.
        string manifestPath = Path.Combine(folder, "dataset.json");
        var manifest = JsonSerializer.Deserialize<ManifestJson>(File.ReadAllText(manifestPath), Read)!;
        string line = MessageText.Format(RunnerMessage.SamplesChanged,
            new { Date = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) });
        var next = manifest with
        {
            Version = manifest.Version + 1,
            ContentHash = DatasetHash.Of(scenarios),
            Changes = [.. manifest.Changes, line]
        };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(next, Write) + "\n");
        return files.Count;
    }

    /// <summary>One old scenario file's text as the Sample file's text.</summary>
    private static string Sample(string old)
    {
        var scenario = JsonSerializer.Deserialize<OldScenario>(old, Read)!;
        var sample = new SampleFile
        {
            Id = scenario.Id,
            Input =
            [
                new SampleMessage
                {
                    Role = "user",
                    Content = scenario.Input.Messages[0]
                }
            ],
            Metadata = new SampleMetadata
            {
                Place = scenario.Input.Place,
                TimeZone = scenario.Input.TimeZone,
                FollowUps = [.. scenario.Input.Messages.Skip(1)],
                Expected = scenario.Expected,
                Categories = scenario.Categories
            }
        };
        return JsonSerializer.Serialize(sample, Write) + "\n";
    }

    /// <summary>A scenario file in the old shape.</summary>
    private sealed record OldScenario(string Id, OldInput Input, JsonElement Expected)
    {
        /// <summary>Its categories; an old file could leave them out.</summary>
        public IReadOnlyList<string> Categories { get; init; } = [];
    }

    /// <summary>The old shape's input: the place, the time zone, and every message in order.</summary>
    private sealed record OldInput(string? Place, string TimeZone, IReadOnlyList<string> Messages);

    /// <summary>A dataset's manifest, as <c>dataset.json</c> holds it.</summary>
    private sealed record ManifestJson(
        string Name,
        int Version,
        string Covers,
        string ContentHash,
        IReadOnlyList<string> Changes);
}
