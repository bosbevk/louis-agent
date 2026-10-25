using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Protocol;

// The harness writes a turn's failure, the API reads and stores it (F18 step 5.2 review): the two sides name the same set, in the same
// words, so a failure added to one side only fails here and not in a stored run.
public class FailureNameTests
{
    [Test]
    public void TheHarnessAndTheApiNameTheSameFailures_InTheSameWords()
    {
        var harness = Enum.GetValues<TrialFailure>().Select(FailureName.Of).Order().ToList();
        var api = Enum.GetValues<BenchmarkTurnFailure>().Select(BenchmarkTurnFailureName.Of).Order().ToList();

        Assert.That(api, Is.EqualTo(harness));
    }
}
