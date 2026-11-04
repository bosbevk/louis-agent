using LouisAgent.Api.Config;

namespace LouisAgent.Api.Tests.Config;

/// <summary>
/// The admin portal's settings are read in one place, the way the agent's own options are: a blank value counts as
/// no value, and every value is trimmed.
/// </summary>
public class AdminOptionsTests
{
    /// <summary>Reads the options from just the variables you give it, as if nothing else were set.</summary>
    private static AdminOptions From(params (string Name, string Value)[] variables)
    {
        var map = variables.ToDictionary(v => v.Name, v => v.Value);
        return AdminOptions.FromEnvironment(name => map.GetValueOrDefault(name));
    }

    /// <summary>With nothing set the portal stays shut, there's no store, and the rest take their defaults.</summary>
    [Test]
    public void WithNothingSet_ThePortalIsClosed_AndThereIsNoStore()
    {
        var options = From();

        Assert.That(options.AdminKey, Is.Null, "no key: the portal stays closed (F27)");
        Assert.That(options.BenchmarkApiUrl, Is.EqualTo("http://127.0.0.1:8080/"), "the container's own port");
        Assert.That(options.EvalsMongoUrl, Is.Null);
        Assert.That(options.EvalsDatabase, Is.EqualTo("evals"), "the store names no product (ADR 0037)");
    }

    /// <summary>The admin key is its own when set, and falls back to the agent's key when it's blank.</summary>
    [Test]
    public void TheAdminKey_IsAdminApiKey_ElseTheAgentsKey()
    {
        Assert.That(From(("ADMIN_API_KEY", " admin "), ("AGENT_API_KEY", "agent")).AdminKey, Is.EqualTo("admin"));
        Assert.That(From(("ADMIN_API_KEY", "  "), ("AGENT_API_KEY", " agent ")).AdminKey, Is.EqualTo("agent"));
    }

    /// <summary>Each setting is picked up, with the spaces around it gone.</summary>
    [Test]
    public void EverySetting_IsReadAndTrimmed()
    {
        var options = From(
            ("BENCHMARK_API_URL", " http://agent:8080/ "),
            ("EVALS_MONGO_URL", " mongodb://louis:pw@mongo:27017 "),
            ("EVALS_MONGO_DATABASE", " evals_test "));

        Assert.That(options.BenchmarkApiUrl, Is.EqualTo("http://agent:8080/"));
        Assert.That(options.EvalsMongoUrl, Is.EqualTo("mongodb://louis:pw@mongo:27017"));
        Assert.That(options.EvalsDatabase, Is.EqualTo("evals_test"));
    }

    /// <summary>
    /// The store's old setting names still work, so an instance whose config hasn't moved keeps its runs where they
    /// are; a new name set beside an old one wins.
    /// </summary>
    [Test]
    public void TheStoresOldNames_AreStillRead_AndTheNewOnesWin()
    {
        var old = From(("BENCHMARKS_MONGO_URL", "mongodb://old"), ("BENCHMARKS_MONGO_DATABASE", "kept_runs"));
        var both = From(("BENCHMARKS_MONGO_URL", "mongodb://old"), ("EVALS_MONGO_URL", "mongodb://new"),
            ("BENCHMARKS_MONGO_DATABASE", "kept_runs"), ("EVALS_MONGO_DATABASE", "evals_runs"));

        Assert.That((old.EvalsMongoUrl, old.EvalsDatabase), Is.EqualTo(("mongodb://old", "kept_runs")));
        Assert.That((both.EvalsMongoUrl, both.EvalsDatabase), Is.EqualTo(("mongodb://new", "evals_runs")));
    }

    /// <summary>A Mongo address that's only spaces means there's no store, under either name.</summary>
    [TestCase("EVALS_MONGO_URL")]
    [TestCase("BENCHMARKS_MONGO_URL")]
    public void ABlankMongoUrl_IsNoStore(string name) =>
        Assert.That(From((name, "   ")).EvalsMongoUrl, Is.Null);

    /// <summary>EVAL_RUNNER says where the eval runner is.</summary>
    [Test]
    public void TheEvalRunnerIsReadFromEVAL_RUNNER() =>
        Assert.That(From(("EVAL_RUNNER", "/x/louis-agent.evals.runner.dll")).EvalRunner,
            Is.EqualTo(Path.GetFullPath("/x/louis-agent.evals.runner.dll")));

    /// <summary>Without EVAL_RUNNER we look where the image puts the runner.</summary>
    [Test]
    public void TheEvalRunnerDefaultsToTheImagesPath() =>
        Assert.That(From().EvalRunner, Is.EqualTo("/app/evals/louis-agent.evals.runner.dll"));

    /// <summary>A relative runner path is made absolute as soon as it's read, so it can't drift later.</summary>
    [Test]
    public void ARelativeEvalRunnerIsMadeAbsoluteWhereItEnters() =>
        Assert.That(From(("EVAL_RUNNER", "../evals/x.dll")).EvalRunner, Is.EqualTo(Path.GetFullPath("../evals/x.dll")));
}
