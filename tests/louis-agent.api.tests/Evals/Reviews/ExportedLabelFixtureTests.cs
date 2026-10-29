using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Reviews;

/// <summary>
/// Makes sure the label the API exports still matches the file the eval harness reads, so neither side can drift
/// without a test failing.
/// </summary>
/// <remarks>
/// Golden file. The API and the harness each have their own record for a label (the API doesn't reference the
/// harness; CL22 will make them one type), so they meet only on the wire. This test checks the API still writes the
/// checked-in fixture exactly, and louis-agent.evals.tests loads that same file with the harness's own loader: change
/// the wire and both fail until the fixture and the two records agree. LOUIS_UPDATE_GOLDEN=1 rewrites the fixture;
/// review its diff before committing.
/// </remarks>
public class ExportedLabelFixtureTests
{
    /// <summary>The checked-in label file both sides are held to.</summary>
    private static readonly string Fixture = System.IO.Path.Combine(ApiTestHost.RepoRoot, "tests",
        "louis-agent.evals.tests", "Labels", "Fixtures", "exported-label.json");

    /// <summary>A person's pass on one conversation, the grade the label is written from.</summary>
    private static readonly HumanGrade Grade =
        new("8f7802b9a72e", new ConversationKey(SampleMode.Beta, 1, "sample-dev"), "meal-for-people",
            HumanVerdict.Pass, "feeds four", "Louis", DateTimeOffset.UnixEpoch);

    /// <summary>A two-turn conversation, since a label carries every turn of its conversation.</summary>
    private static readonly Turn[] Turns =
    [
        new(new TurnKey(SampleMode.Beta, 1, "sample-dev", 1), "sess_1", "Hello", "Bobotie vir vier",
            SampleRole.Alpha, null, 1, 1, [], 1.0, null),
        new(new TurnKey(SampleMode.Beta, 1, "sample-dev", 2), "sess_1", "Make it six",
            "Bobotie vir ses", SampleRole.Alpha, null, 1, 1, [], 1.0, null),
    ];

    /// <summary>The label as the API writes it to a file.</summary>
    private static string Written() => ReviewLabels.Content(Grade, Turns);

    /// <summary>
    /// The one-line label we hand the runner on stdin is the file's JSON without line breaks, nothing more or less.
    /// </summary>
    [Test]
    public void TheLine_IsTheFileOnOneLine()
    {
        string line = ReviewLabels.Line(Grade, Turns);

        Assert.That(line, Does.Not.Contain("\n").And.Not.Contain("\r"));
        // Compare parsed JSON, so only the content counts, not the layout.
        Assert.That(System.Text.Json.Nodes.JsonNode.Parse(line)!.ToJsonString(),
            Is.EqualTo(System.Text.Json.Nodes.JsonNode.Parse(Written())!.ToJsonString()));
    }

    /// <summary>The API still writes the fixture exactly (line endings aside), or rewrites it when asked to.</summary>
    [Test]
    public void TheAPIStillWritesExactlyTheFixtureTheHarnessLoads()
    {
        // With LOUIS_UPDATE_GOLDEN=1 we write the fixture instead of checking it.
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

    /// <summary>
    /// A label's file is named the way the harness names its own sheets: grader, scenario, mode, run, repeat.
    /// </summary>
    [Test]
    public void TheFileNameIsGraderScenarioModeRunRepeat_ThatTheHarnessUsesForItsOwnSheets()
    {
        var key = new ConversationKey(SampleMode.Gamma, 2, SampleScenario.Alpha);

        Assert.That(ReviewLabels.FileName("meal-for-people", key, "8f7802b9a72e"),
            Is.EqualTo(SampleScenario.Fill($"meal-for-people.<ScenarioAlpha>.{SampleMode.Gamma}.8f7802b9a72e.2.json")));
    }
}
