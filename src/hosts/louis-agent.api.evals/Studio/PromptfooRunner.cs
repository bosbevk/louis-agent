using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.List;

namespace LouisAgent.Api.Evals.Studio;

/// <summary>
/// What a promptfoo import gave: the eval's file, its rubrics and its scenario files as the runner wrote them, and each
/// thing the runner left out, in its words; or why the runner gave nothing.
/// </summary>
public sealed record PromptfooImported(
    string Json,
    IReadOnlyList<RubricFile> Rubrics,
    IReadOnlyList<RubricFile> Scenarios,
    IReadOnlyList<string> LeftOut,
    string? Failure);

/// <summary>
/// Has the eval runner turn a promptfoo file into an eval folder (<c>--import-promptfoo {file} {folder}</c>) and an
/// eval folder into a promptfoo file (<c>--export-promptfoo {folder}</c>), and reads what it wrote back as the files a
/// draft keeps.
/// </summary>
/// <remarks>
/// The runner is the one reader and writer of eval files (finding R), reached through <see cref="RunnerProcess"/> as
/// <see cref="DraftChecker"/> is; the process is a function handed in, so a test answers as a runner would with no
/// runner at all. The import works in a folder of its own under the scratch folder, deleted when it is read.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class PromptfooRunner(Func<IReadOnlyList<string>, RunnerOutput> run)
{
    /// <summary>The bridge over the runner at this path; neither command takes more than a minute.</summary>
    public static PromptfooRunner ForRunner(string runnerPath) =>
        new(args => RunnerProcess.Run(EvalListing.DotNetHost, [runnerPath, .. args], TimeSpan.FromMinutes(1),
            runnerPath, args[0]));

    /// <summary>The promptfoo file's eval as files a draft keeps, written and read in a folder under the scratch.</summary>
    public PromptfooImported Import(string yaml, string scratch)
    {
        string work = Path.Combine(scratch, "promptfoo-" + Guid.NewGuid().ToString("N"));
        string file = Path.Combine(work, "promptfooconfig.yaml"), folder = Path.Combine(work, "eval");
        Directory.CreateDirectory(work);
        try
        {
            File.WriteAllText(file, yaml);
            var output = run(["--import-promptfoo", file, folder]);
            if (output.Failure is { } failure) return new PromptfooImported("", [], [], [], failure);
            return new PromptfooImported(File.ReadAllText(Path.Combine(folder, "eval.json")),
                FilesIn(folder, "Rubrics", "*.md"),
                FilesIn(folder, Path.Combine("Dataset", "scenarios", "dev"), "*.json"),
                LeftOutLines(output.Stdout), null);
        }
        finally
        {
            Directory.Delete(work, recursive: true);
        }
    }

    /// <summary>The promptfoo file for the eval folder at this path, or why the runner gave none.</summary>
    public RunnerOutput Export(string folder) => run(["--export-promptfoo", folder]);

    /// <summary>Every file of this pattern in one of the folder's folders, by name, none when it isn't there.</summary>
    private static IReadOnlyList<RubricFile> FilesIn(string folder, string under, string pattern)
    {
        string path = Path.Combine(folder, under);
        if (!Directory.Exists(path)) return [];
        return
        [
            .. Directory.GetFiles(path, pattern).Order(StringComparer.Ordinal)
                .Select(file => new RubricFile(Path.GetFileName(file), File.ReadAllText(file)))
        ];
    }

    /// <summary>What the runner left out: one <c>{"left_out": …}</c> line each.</summary>
    private static IReadOnlyList<string> LeftOutLines(string stdout) =>
    [
        .. stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => JsonSerializer.Deserialize<LeftOutLine>(line, LeftOutJson)?.LeftOut ?? "")
            .Where(left => left.Length > 0)
    ];

    /// <summary>The runner's snake_case lines.</summary>
    private static readonly JsonSerializerOptions LeftOutJson =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>One thing the runner left out, as its line says it.</summary>
    private sealed record LeftOutLine(string? LeftOut);
}
