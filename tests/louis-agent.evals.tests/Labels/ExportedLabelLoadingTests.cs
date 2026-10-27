using LouisAgent.Testing;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Tests.Datasets;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

// F18 step 6a.2: the harness's loader accepts what the API exports. The fixture is the file the API's own test writes (ExportedLabelFixtureTests, checked in), so the two records on one
// wire cannot drift apart without a test failing on one side or the other. Written first: the API had no label export.
public class ExportedLabelLoadingTests
{
    private sealed class MealGrader : IGrader
    {
        public string Name => "meal-for-people";
        public int Version => 1;
        public GraderKind Kind => GraderKind.Model;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "routed" };

        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Test]
    public void TheFileTheApiExports_IsAccepted_ByTheLoaderThatReadsTheDatasetsLabels()
    {
        string fixture = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.evals.tests", "Labels", "Fixtures",
            "exported-label.json");
        // The fixture is a two-turn conversation (6a.4f), so the scenario has two messages.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/sample-dev.json",
                DatasetFolder.Scenario("sample-dev").Replace("\"messages\": [\"Hello\"]",
                    "\"messages\": [\"Hello\", \"Make it six\"]")).WithManifest()
            .Write($"labels/meal-for-people.sample-dev.{SampleMode.Beta}.8f7802b9a72e.1.json",
                File.ReadAllText(fixture));

        var label = DatasetLoader.Load(folder.Root, [new MealGrader()]).Labels.Single();

        Assert.That(
            (label.Grader, label.Scenario, label.Mode, label.Run, label.Repeat, label.Turn, label.Answer, label.Pass,
                label.Note),
            Is.EqualTo(("meal-for-people", "sample-dev", SampleMode.Beta, "8f7802b9a72e", 1, 2, "Bobotie vir ses", true,
                "feeds four")));
        Assert.That(label.Turns,
            Is.EqualTo(new[]
                { new LabelTurn("Hello", "Bobotie vir vier"), new LabelTurn("Make it six", "Bobotie vir ses") }),
            "6a.4f: the whole conversation");
    }
}
