using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Tests.Endpoints.Routing;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Reviews;

// F18 step 6a.2: the label the API exports and the label the harness's loader reads are two records on one wire (the API doesn't reference the harness; CL22 will make them one type). So a
// drift on either side must fail a test: this one asserts the API still writes exactly the checked-in fixture, and louis-agent.evals.tests loads that same file with the harness's own
// LabelLoader. Change the wire, and both fail until the fixture and the two records agree. LOUIS_UPDATE_GOLDEN=1 rewrites the fixture; review its diff before committing.
public class ExportedLabelFixtureTests
{
    private static readonly string Fixture = System.IO.Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.evals.tests", "Labels", "Fixtures", "exported-label.json");

    private static readonly HumanGrade Grade =
        new("8f7802b9a72e", new BenchmarkProtocol.ConversationKey(SampleMode.Beta, 1, "sample-dev"), "meal-for-people", HumanVerdict.Pass, "feeds four", "Louis", DateTimeOffset.UnixEpoch);

    // 6a.4f: a label carries every turn of its conversation, so the fixture pins a two-turn one.
    private static readonly Turn[] Turns =
    [
        new(new BenchmarkProtocol.TurnKey(SampleMode.Beta, 1, "sample-dev", 1), "sess_1", "Hello", "Bobotie vir vier", SampleRole.Alpha, null, 1, 1, [], 1.0, null),
        new(new BenchmarkProtocol.TurnKey(SampleMode.Beta, 1, "sample-dev", 2), "sess_1", "Make it six", "Bobotie vir ses", SampleRole.Alpha, null, 1, 1, [], 1.0, null),
    ];

    private static string Written() => ReviewLabels.Content(Grade, Turns);

    // 6a.5b: the same label on one line, for the runner's stdin: the file's JSON with no line breaks, nothing more or less.
    [Test]
    public void TheLine_IsTheFileOnOneLine()
    {
        string line = ReviewLabels.Line(Grade, Turns);

        Assert.That(line, Does.Not.Contain("\n").And.Not.Contain("\r"));
        Assert.That(System.Text.Json.Nodes.JsonNode.Parse(line)!.ToJsonString(), Is.EqualTo(System.Text.Json.Nodes.JsonNode.Parse(Written())!.ToJsonString()));
    }

    [Test]
    public void TheAPIStillWritesExactlyTheFixtureTheHarnessLoads()
    {
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Fixture)!);
            File.WriteAllText(Fixture, Written());
            Assert.Pass($"Wrote {Fixture}; review its diff before committing.");
        }

        Assert.That(File.Exists(Fixture), $"No fixture; run with LOUIS_UPDATE_GOLDEN=1 to write {Fixture}.");
        Assert.That(Written().ReplaceLineEndings("\n"), Is.EqualTo(File.ReadAllText(Fixture).ReplaceLineEndings("\n")),
            "The API's label wire changed. The harness's LabelLoader reads this file too: change both, then rewrite the fixture with LOUIS_UPDATE_GOLDEN=1.");
    }

    [Test]
    public void TheFileNameIsGraderScenarioModeRunRepeat_ThatTheHarnessUsesForItsOwnSheets()
    {
        var key = new BenchmarkProtocol.ConversationKey(SampleMode.Gamma, 2, SampleScenario.Alpha);

        Assert.That(ReviewLabels.FileName("meal-for-people", key, "8f7802b9a72e"), Is.EqualTo(SampleScenario.Fill($"meal-for-people.<ScenarioAlpha>.{SampleMode.Gamma}.8f7802b9a72e.2.json")));
    }
}
