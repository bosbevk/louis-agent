using System.Text.Json;
using System.Text.RegularExpressions;

namespace LouisAgent.Api.Services;

/// <summary>
/// The checks the portal can run and the runs saved under <c>samples/</c> (F27). A run is a folder with
/// <c>summary.json</c> (a repeated run: the cost per mode as a mean) or <c>results.json</c> (one run: the cost per
/// mode under "all"); the cost shown is the modes' added, the way the READMEs report it. Reads only the few files the
/// page shows, and never a path outside <c>samples/</c>.
/// </summary>
/// <param name="checks">The checks; null for the built-in two (a test passes its own, with a stand-in script).</param>
public sealed partial class BenchmarkCatalog(string samplesRoot, IReadOnlyList<BenchmarkCheck>? checks = null)
{
    public static readonly IReadOnlyList<BenchmarkCheck> BuiltIn =
    [
        new("cook", "Die kokkontrole", "samples/puls-cook-check/run_check.py", ["sample-output-puls-cook", "sample-output-cook"]),
        new("delegation", "Die delegeringskontrole", "samples/puls-check/run_check.py", ["sample-output-puls-check", "sample-output-delegation"]),
    ];

    public IReadOnlyList<BenchmarkCheck> Checks { get; } = checks ?? BuiltIn;

    public string SamplesRoot { get; } = samplesRoot;

    /// <summary>Every saved run, newest first.</summary>
    public IReadOnlyList<SavedRun> SavedRuns()
    {
        if (!Directory.Exists(SamplesRoot)) return [];
        return Directory.EnumerateDirectories(SamplesRoot, "sample-output-*")
            .Select(Describe)
            .OfType<SavedRun>()
            .OrderByDescending(r => r.SavedAt)
            .ToList();
    }

    /// <summary>One run, by its folder name; null for a name that isn't a run's, or isn't a plain folder name.</summary>
    public SavedRunDetail? Read(string folder)
    {
        if (!FolderName().IsMatch(folder)) return null;
        string path = Path.Combine(SamplesRoot, folder);
        if (Describe(path) is not { } run) return null;
        return new SavedRunDetail(run,
            Summary: ReadIfExists(path, "summary.md") ?? ReadIfExists(path, "results.md"),
            Readme: ReadIfExists(path, "README.md"),
            Results: ReadIfExists(path, "summary.json") ?? ReadIfExists(path, "results.json"),
            Transcripts: Transcripts(path),
            Modes: Modes(path));
    }

    /// <summary>Every mode folder with a ledger, in order: its <c>usage.jsonl</c> and <c>answers.json</c> as saved (empty when missing).</summary>
    private static List<RunMode> Modes(string run) =>
        Directory.EnumerateFiles(run, "usage.jsonl", SearchOption.AllDirectories)
            .Select(file => new RunMode(
                Path.GetRelativePath(run, Path.GetDirectoryName(file)!).Replace('\\', '/'),
                File.ReadAllText(file),
                ReadIfExists(Path.GetDirectoryName(file)!, "answers.json") ?? "[]"))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>Every mode's transcript, in order: <c>{mode}/transcript.md</c>, or <c>run-N/{mode}/transcript.md</c> for a repeated run.</summary>
    private static List<RunTranscript> Transcripts(string run) =>
        Directory.EnumerateFiles(run, "transcript.md", SearchOption.AllDirectories)
            .Select(file => new RunTranscript(
                Path.GetRelativePath(run, Path.GetDirectoryName(file)!).Replace('\\', '/'),
                File.ReadAllText(file)))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

    private SavedRun? Describe(string path)
    {
        string? summary = ReadIfExists(path, "summary.json");
        string? results = summary is null ? ReadIfExists(path, "results.json") : null;
        if (summary is null && results is null) return null;

        string folder = Path.GetFileName(path);
        string? check = Checks.FirstOrDefault(c => c.FolderPrefixes.Any(p => folder.StartsWith(p, StringComparison.Ordinal)))?.Id;
        int repeats = Math.Max(1, Directory.EnumerateDirectories(path, "run-*").Count());
        decimal? cost = summary is not null ? Cost(summary, "cost", "mean") : Cost(results!, "all", "cost");
        return new SavedRun(folder, check, Directory.GetLastWriteTimeUtc(path), repeats, cost);
    }

    /// <summary>The modes' costs added: each top-level object's <paramref name="first"/>.<paramref name="second"/>.</summary>
    private static decimal? Cost(string json, string first, string second)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            decimal? total = null;
            foreach (var mode in doc.RootElement.EnumerateObject())
            {
                if (mode.Value.ValueKind != JsonValueKind.Object) continue;
                if (mode.Value.TryGetProperty(first, out var a) && a.ValueKind == JsonValueKind.Object
                    && a.TryGetProperty(second, out var b) && b.ValueKind == JsonValueKind.Number)
                    total = (total ?? 0) + b.GetDecimal();
            }
            return total;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadIfExists(string folder, string file)
    {
        string path = Path.Combine(folder, file);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial Regex FolderName();
}
