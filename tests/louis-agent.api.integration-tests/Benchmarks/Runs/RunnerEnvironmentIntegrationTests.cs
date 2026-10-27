using System.Text.RegularExpressions;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Runs;

// FX25: the runner's process got the API's whole environment (.NET hands a child the parent's by default), so a run could read the Mongo login, the admin key and every
// provider key, of which it reads none. Now it gets an allowlist: what the runtime needs to start, what the runner reads, and what the API sets for the
// run. An Anthropic judge with no key of its own gets the Anthropic key as the judge's key, under that name only. Written first: the start info copied everything.
[Category("Integration")]
public class RunnerEnvironmentIntegrationTests
{
    private static readonly BenchmarkCheck Stub = new BenchmarkCheck("stub", "Stub");

    [Test]
    public void TheStartInfo_CarriesTheSelection_AndTheRunsOwnVariables()
    {
        string? mongo = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");
        Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", "mongodb://user:secret@mongo/puls");
        try
        {
            var start = new BenchmarkRunner(new BenchmarkCatalog([Stub]), TestPaths.RepoRoot, "http://127.0.0.1:1/",
                    RunnerBuild.Dll)
                .StartInfoFor(Stub, new BenchmarkRun("run123456789", "stub", 1));

            Assert.That(start.Environment.ContainsKey("BENCHMARKS_MONGO_URL"), Is.False);
            Assert.That(start.Environment["PULS_API"], Is.EqualTo("http://127.0.0.1:1/"));
            Assert.That(start.Environment["BENCHMARK_RUN"], Is.EqualTo("run123456789"));
            Assert.That(start.Environment.ContainsKey("PATH") || start.Environment.ContainsKey("Path"), Is.True,
                "the runtime's path comes through");
        }
        finally
        {
            Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", mongo);
        }
    }

    /// <summary>The real thing, on a real child process (the stand-in for the runner, F18 step 7.4): what the API starts sees none of the API's secrets, still has the path it needs, and gets what the run is told.</summary>
    [Test]
    public async Task AChildStartedByTheRunner_SeesNoSecret_AndStillStarts()
    {
        string? mongo = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");
        string? admin = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        string? anthropic = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", "mongodb://user:secret@mongo/puls");
        Environment.SetEnvironmentVariable("ADMIN_API_KEY", "admin-secret");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-secret");
        try
        {
            var runner = new BenchmarkRunner(new BenchmarkCatalog([new BenchmarkCheck("env", "Env")]),
                Path.GetTempPath(), "http://x/", StandInBuild.Dll);
            var run = runner.Start("env", 1)!;
            var lines = new List<string>();
            await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);

            Assert.That(run.ExitCode, Is.EqualTo(0), string.Join(" | ", lines));
            var names = lines.Where(l => l.StartsWith("env:")).Select(l => l[4..]).ToList();
            Assert.That(names,
                Has.None.EqualTo("BENCHMARKS_MONGO_URL").And.None.EqualTo("ADMIN_API_KEY").And.None
                    .EqualTo("ANTHROPIC_API_KEY"), "the API's secrets never reach the child");
            Assert.That(names.Any(n => n.Equals("PATH", StringComparison.OrdinalIgnoreCase)), Is.True,
                "the path the runtime needs comes through");
            Assert.That(lines, Does.Contain("api=http://x/").And.Contain($"run={run.Id}"),
                "what the API tells the run arrives");
        }
        finally
        {
            Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", mongo);
            Environment.SetEnvironmentVariable("ADMIN_API_KEY", admin);
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", anthropic);
        }
    }
}
