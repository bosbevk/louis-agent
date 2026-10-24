using LouisAgent.Api.Config;

namespace LouisAgent.Api.Tests.Config;

// F31 step 5a: the admin portal's settings read in one place, on AgentOptions.FromEnvironment's pattern (a blank value
// is no value, every value trimmed), instead of three reads scattered through Program.cs.
public class AdminOptionsTests
{
    private static AdminOptions From(params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value);
        return AdminOptions.FromEnvironment(name => map.GetValueOrDefault(name));
    }

    [Test]
    public void WithNothingSet_ThePortalIsClosed_AndThereIsNoStore()
    {
        var options = From();

        Assert.That(options.AdminKey, Is.Null, "no key: the portal stays closed (F27)");
        Assert.That(options.BenchmarkApiUrl, Is.EqualTo("http://127.0.0.1:8080/"), "the container's own port");
        Assert.That(options.BenchmarksMongoUrl, Is.Null);
        Assert.That(options.BenchmarksDatabase, Is.EqualTo("puls_benchmarks"));
    }

    [Test]
    public void TheAdminKey_IsAdminApiKey_ElseTheAgentsKey()
    {
        Assert.That(From(("ADMIN_API_KEY", " admin "), ("AGENT_API_KEY", "agent")).AdminKey, Is.EqualTo("admin"));
        Assert.That(From(("ADMIN_API_KEY", "  "), ("AGENT_API_KEY", " agent ")).AdminKey, Is.EqualTo("agent"));
    }

    [Test]
    public void EverySetting_IsReadAndTrimmed()
    {
        var options = From(
            ("BENCHMARK_API_URL", " http://puls:8080/ "),
            ("BENCHMARKS_MONGO_URL", " mongodb://louis:pw@puls-mongo:27017 "),
            ("BENCHMARKS_MONGO_DATABASE", " puls_benchmarks_test "));

        Assert.That(options.BenchmarkApiUrl, Is.EqualTo("http://puls:8080/"));
        Assert.That(options.BenchmarksMongoUrl, Is.EqualTo("mongodb://louis:pw@puls-mongo:27017"));
        Assert.That(options.BenchmarksDatabase, Is.EqualTo("puls_benchmarks_test"));
    }

    [Test]
    public void ABlankMongoUrl_IsNoStore() =>
        Assert.That(From(("BENCHMARKS_MONGO_URL", "   ")).BenchmarksMongoUrl, Is.Null);

    [Test]
    public void TheEvalRunnerIsReadFromEVAL_RUNNER() =>
        Assert.That(From(("EVAL_RUNNER", "/x/louis-agent.evals.runner.dll")).EvalRunner, Is.EqualTo(Path.GetFullPath("/x/louis-agent.evals.runner.dll")));

    [Test]
    public void TheEvalRunnerDefaultsToTheImagesPath() =>
        Assert.That(From().EvalRunner, Is.EqualTo("/app/evals/louis-agent.evals.runner.dll"));

    [Test]
    public void ARelativeEvalRunnerIsMadeAbsoluteWhereItEnters() =>
        Assert.That(From(("EVAL_RUNNER", "../evals/x.dll")).EvalRunner, Is.EqualTo(Path.GetFullPath("../evals/x.dll")));
}
