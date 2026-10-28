using System.Text.RegularExpressions;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Runs;

/// <summary>
/// The runner's child process gets only what it needs from the API's environment: what the runtime needs to start,
/// what the runner reads, and what the API sets for the run. The Mongo login, the admin key and the provider keys
/// stay behind (FX25).
/// </summary>
[Category("Integration")]
public class RunnerEnvironmentIntegrationTests
{
    /// <summary>The one eval the runner knows in the first test.</summary>
    private static readonly BenchmarkCheck Stub = new BenchmarkCheck("stub", "Stub");

    /// <summary>The start info drops the Mongo login but keeps the path and what the API tells the run.</summary>
    [Test]
    public void TheStartInfo_CarriesTheSelection_AndTheRunsOwnVariables()
    {
        // Put a Mongo login in this process's environment, and put back whatever was there afterwards.
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

    /// <summary>
    /// The real thing, on a real child process (a stand-in for the runner that prints what it sees): it sees none of
    /// the API's secrets, still has the path it needs, and gets what the run is told.
    /// </summary>
    [Test]
    public async Task AChildStartedByTheRunner_SeesNoSecret_AndStillStarts()
    {
        // Put three secrets in this process's environment, and put back whatever was there afterwards.
        string? mongo = Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");
        string? admin = Environment.GetEnvironmentVariable("ADMIN_API_KEY");
        string? anthropic = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        Environment.SetEnvironmentVariable("BENCHMARKS_MONGO_URL", "mongodb://user:secret@mongo/puls");
        Environment.SetEnvironmentVariable("ADMIN_API_KEY", "admin-secret");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "sk-ant-secret");
        try
        {
            // Start the stand-in and collect every line it prints until it ends.
            var runner = new BenchmarkRunner(new BenchmarkCatalog([new BenchmarkCheck("env", "Env")]),
                Path.GetTempPath(), "http://x/", StandInBuild.Dll);
            var run = runner.Start("env", 1)!;
            var lines = new List<string>();
            await foreach (string line in runner.EventsAsync(run.Id)) lines.Add(line);

            // It prints each variable's name it sees as "env:NAME".
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
