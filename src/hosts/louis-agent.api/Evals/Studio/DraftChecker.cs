using System.Text.Json;
using LouisAgent.Api.Evals.List;

namespace LouisAgent.Api.Evals.Studio;

/// <summary>What the runner said of a folder: whether it conforms and every problem, or why it couldn't look.</summary>
public sealed record DraftCheck(bool Ok, IReadOnlyList<string> Problems, string? Failure);

/// <summary>
/// Asks the eval runner to judge a folder the studio wrote (<c>dotnet {EVAL_RUNNER} --check {folder}</c>) and reads its
/// lines: one per problem, then whether it was ok.
/// </summary>
/// <remarks>
/// The runner is the one judge of eval files (finding R), reached through <see cref="RunnerProcess"/> as the
/// listings are. The process is a function handed in, so a test answers with the lines a runner writes and no runner
/// at all. Two minutes is the most a check may take: it loads the catalogue and grades every transcript.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class DraftChecker(Func<string, RunnerOutput> run)
{
    /// <summary>A checker over the runner at this path.</summary>
    public static DraftChecker ForRunner(string runnerPath) =>
        new(folder => RunnerProcess.Run(EvalListing.DotNetHost, [runnerPath, "--check", folder],
            TimeSpan.FromMinutes(2), runnerPath, "--check"));

    /// <summary>What the runner said of the folder.</summary>
    public DraftCheck Check(string folder)
    {
        var output = run(folder);
        if (output.Failure is { } failure) return new DraftCheck(false, [], failure);

        var problems = new List<string>();
        bool ok = false;
        foreach (string line in output.Stdout.Split('\n',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.TryGetProperty("problem", out var problem)) problems.Add(problem.GetString() ?? "");
            if (root.TryGetProperty("ok", out var said)) ok = said.GetBoolean();
        }

        return new DraftCheck(ok && problems.Count == 0, problems, null);
    }
}
