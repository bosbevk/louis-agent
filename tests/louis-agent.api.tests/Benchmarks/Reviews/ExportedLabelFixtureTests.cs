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

    private static string Written()
    {
        var key = new BenchmarkProtocol.ConversationKey("puls-cook", 1, "sample-dev");
        var grade = new HumanGrade("8f7802b9a72e", key, "meal-for-people", HumanVerdict.Pass, "feeds four", "Louis", DateTimeOffset.UnixEpoch);
        // 6a.4f: a label carries every turn of its conversation, so the fixture pins a two-turn one.
        var turns = new[]
        {
            new Turn(new BenchmarkProtocol.TurnKey("puls-cook", 1, "sample-dev", 1), "sess_1", "Hello", "Bobotie vir vier", "puls-cook", null, 1, 1, [], 1.0, null),
            new Turn(new BenchmarkProtocol.TurnKey("puls-cook", 1, "sample-dev", 2), "sess_1", "Make it six", "Bobotie vir ses", "puls-cook", null, 1, 1, [], 1.0, null),
        };

        return ReviewLabels.Content(grade, turns);
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
        var key = new BenchmarkProtocol.ConversationKey("puls-tools", 2, "cook-af");

        Assert.That(ReviewLabels.FileName("meal-for-people", key, "8f7802b9a72e"), Is.EqualTo("meal-for-people.cook-af.puls-tools.8f7802b9a72e.2.json"));
    }
}
