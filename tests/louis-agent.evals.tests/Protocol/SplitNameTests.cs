using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Tests.Protocol;

// The harness writes a run's split, the API reads and stores it (F18 step 5.4 review): the two sides name the same choices in the same words, so a split
// added to one side only fails here and not in a stored run.
public class SplitNameTests
{
    [Test]
    public void TheHarnessAndTheApiNameTheSameSplits_InTheSameWords()
    {
        var harness = Enum.GetValues<SplitSelection>().Select(SplitSelections.WireName).Order().ToList();
        var api = Enum.GetValues<BenchmarkRunSplit>().Select(BenchmarkRunSplitName.Of).Order().ToList();

        Assert.That(api, Is.EqualTo(harness));
    }
}
