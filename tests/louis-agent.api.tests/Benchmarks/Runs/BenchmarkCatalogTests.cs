
using LouisAgent.Api.Benchmarks.Runs;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F27 step 1 (F27-S3): the checks the portal can run; F31 brings them back, each with its script under benchmarks/.
public class BenchmarkCatalogTests
{
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void AnEmptyCatalog_HasNoChecks() =>
        Assert.That(new BenchmarkCatalog().Checks, Is.Empty);

    [Test]
    public void ThePortalsChecks_AreTheStubTheCookAndTheRouting_EachWithItsScript()
    {
        // F31 steps 6c, 7b and 8.
        Assert.That(BenchmarkCatalog.Default.Select(c => c.Id), Is.EqualTo(new[] { "stub", "cook", "routing" }));
        foreach (var check in BenchmarkCatalog.Default)
            Assert.That(File.Exists(Path.Combine(RepoRoot, check.Script)), check.Script);
    }

    [Test]
    public void ACatalog_OffersTheChecksItIsGiven()
    {
        var check = new BenchmarkCheck("cook", "Die kokkontrole", "benchmarks/cook/run_check.py", ["--x"]);

        Assert.That(new BenchmarkCatalog([check]).Checks, Is.EqualTo(new[] { check }));
    }

    [Test]
    public void AnEvalCheckHasItsEvalsManual() =>
        Assert.That(BenchmarkCheck.ForEval("stub", "Stub").ManualPath, Is.EqualTo("benchmarks/stub/HANDLEIDING.md"));

    [Test]
    public void AListedEvalHidesThePythonCheckWithItsId()
    {
        var merged = BenchmarkCatalog.Merge([new EvalLine("stub", "puls", 1, "A canned run")], BenchmarkCatalog.Default);

        Assert.That(merged.Select(c => c.Id), Is.EqualTo(new[] { "stub", "cook", "routing" }));
        Assert.That((merged[0].Kind, merged[0].Title), Is.EqualTo((BenchmarkCheckKind.Eval, "A canned run")));
        Assert.That(merged.Skip(1).All(c => c.Kind == BenchmarkCheckKind.Script), Is.True);
    }

    [Test]
    public void WithNoListedEvals_ThePythonChecksStay() =>
        Assert.That(BenchmarkCatalog.Merge([], BenchmarkCatalog.Default).Select(c => c.Id), Is.EqualTo(new[] { "stub", "cook", "routing" }));

    [Test]
    public void AListingThatFails_KeepsThePythonChecksAndSaysWhy()
    {
        var log = new CapturingLogger();

        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), BenchmarkCatalog.Default, log);

        Assert.That(catalog.Checks.Select(c => c.Id), Is.EqualTo(new[] { "stub", "cook", "routing" }));
        Assert.That(log.Messages, Has.Some.Contains("isn't at /nope/louis-agent.evals.runner.dll"));
    }

    [Test]
    public void TheListingIsReadOnFirstUse()
    {
        var log = new CapturingLogger();

        var catalog = BenchmarkCatalog.From(EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll"), BenchmarkCatalog.Default, log);
        Assert.That(log.Messages, Is.Empty, "nothing runs until the checks are asked for");

        _ = catalog.Checks;
        Assert.That(log.Messages, Is.Not.Empty);
    }

    /// <summary>Keeps what the catalogue logs, formatted, so a test can read the warning.</summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
