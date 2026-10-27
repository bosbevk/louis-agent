using LouisAgent.Choices;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Admin;

/// <summary>
/// The last anonymous row an admin endpoint still sends: a live run, for the re-score's 201, until that file is swept.
/// Its record twin is <c>LiveRunRow</c>.
/// </summary>
/// <remarks>
/// On its way out: the endpoint sweep turns each row into a contract record built by its <c>Of</c>
/// (<c>Admin/Replies</c>), and a row goes from here when its last caller is swept.
/// </remarks>
internal static class AdminRows
{
    /// <summary>
    /// A live run: the runner's view, and whether its recording kept everything once it is done. A judge check says
    /// which run and judge it checks, and its last line, since its lines are not streamed to a page.
    /// </summary>
    public static object Row(BenchmarkRun run, BenchmarkRecorder.Result? stored = null) => new
    {
        id = run.Id,
        check = run.Check,
        repeat = run.Repeat,
        status = run.Status.Wire(),
        exit_code = run.ExitCode,
        started_at = run.StartedAt,
        ended_at = run.EndedAt,
        lines = run.Lines.Count,
        stored = stored?.Stored,
        store_error = stored?.StoreError,
        calibration_of = run.Calibration?.Of,
        grader = run.Calibration?.Grader,
        last_line = run.Calibration is null ? null : run.Lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l)),
    };
}
