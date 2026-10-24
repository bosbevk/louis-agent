using LouisAgent.Evals.Runner.Arguments;

namespace LouisAgent.Evals.Runner.Tests.Arguments;

// The run's name (F18 step 4.1 review): the runner reads BENCHMARK_RUN, as the Python does, so a real eval's ledger rows find
// their run. Written first: the reader threw NotImplementedException on the red run.
public class BenchmarkRunTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UnsetIsLocal(string? value) =>
        Assert.That(BenchmarkRun.Of(value), Is.EqualTo("local"));

    [Test]
    public void TheVariableIsTheRunTrimmed() =>
        Assert.That(BenchmarkRun.Of(" portal-run-7 "), Is.EqualTo("portal-run-7"));
}
