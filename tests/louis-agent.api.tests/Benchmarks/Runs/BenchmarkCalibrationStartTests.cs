using System.Text.Json;
using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Contracts.Admin;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>
/// A judge check is started like a re-score (F18 step 6a.5b): the eval runner as a child process with an argument list
/// (ADR 0004), and the label lines on its stdin. It is the runner's one run at a time, and its row says what it is,
/// so the Benchmarks page can leave it alone.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public class BenchmarkCalibrationStartTests
{
    /// <summary>
    /// A judge check's row names the run and the judge it checks and its last line; an ordinary run's names none.
    /// </summary>
    [Test]
    public void TheRow_SaysWhatRunItChecksAndWhichJudge_AndItsLastLine()
    {
        var run = new BenchmarkRun("cal123456789", "stub", 1,
            calibration: new BenchmarkCalibration("76667d39dcdb", "meal-for-people"), input: "{}\n");
        run.Add("Agreement: 36 of 36");

        var row = JsonSerializer.SerializeToElement(LiveRunRow.Of(run), AdminJson.SnakeCase);
        var ordinary = JsonSerializer.SerializeToElement(LiveRunRow.Of(new BenchmarkRun("run123456789", "stub", 1)),
            AdminJson.SnakeCase);

        Assert.That(
            (row.GetProperty("calibration_of").GetString(), row.GetProperty("grader").GetString(),
                row.GetProperty("last_line").GetString()),
            Is.EqualTo(("76667d39dcdb", "meal-for-people", "Agreement: 36 of 36")));
        Assert.That(ordinary.GetProperty("calibration_of").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(ordinary.GetProperty("last_line").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "an ordinary run's lines are streamed, not summarised");
    }
}
