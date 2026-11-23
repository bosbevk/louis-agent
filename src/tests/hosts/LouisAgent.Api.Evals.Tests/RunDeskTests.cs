using LouisAgent.Evals.Contracts.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// The desk knows the one run that is going, so a stored run that still says running and isn't that one was cut
/// off by a restart, and reads as could not finish with no clean-up job.
/// </summary>
public class RunDeskTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);

    /// <summary>A stored run that says running, while the desk runs nothing, reads as could not finish.</summary>
    [Test]
    public void AStoredRunningRun_ThatIsntTheDesksOwn_ReadsAsCouldNotFinish()
    {
        var desk = new RunDesk(NullLogger<RunDesk>.Instance);
        var stored = new EvalRun("20261123-043012-greets", "greets", "an-eval-agent", RunKind.Live, 1, Started, []);

        var read = desk.Read(stored);

        Assert.That(read.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(read.StoppedBecause, Is.Not.Empty);
    }

    /// <summary>A run that ended reads as it is.</summary>
    [Test]
    public void AnEndedRun_ReadsAsItIs()
    {
        var desk = new RunDesk(NullLogger<RunDesk>.Instance);
        var stored = new EvalRun("20261123-043012-greets", "greets", "an-eval-agent", RunKind.Live, 1, Started, [])
        {
            Status = RunStatus.Finished
        };

        Assert.That(desk.Read(stored), Is.SameAs(stored));
    }
}
