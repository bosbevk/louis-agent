using LouisAgent.Testing;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Tests.Datasets;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Labels;

/// <summary>
/// The harness's loader accepts what the API exports (F18 step 6a.2). The fixture is the file the API's own test
/// writes (<c>ExportedLabelFixtureTests</c>, checked in), so the two records on one wire cannot drift apart without a
/// test failing on one side or the other.
/// </summary>
public class ExportedLabelLoadingTests
{
    /// <summary>The model grader the label answers; it never grades here.</summary>
    private sealed class MealCheck : ICheck
    {
        /// <summary>Its name.</summary>
        public string Name => "meal-for-people";

        /// <summary>Its version.</summary>
        public int Version => 1;

        /// <summary>Its kind: a model, so it can be labelled.</summary>
        public CheckKind Kind => CheckKind.Model;

        /// <summary>Not required.</summary>
        public bool Required => false;

        /// <summary>The key it reads.</summary>
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "routed" };

        /// <summary>Never called: the loader only reads its name and keys.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    /// <summary>The file the API exports loads as the dataset's label, with the whole conversation.</summary>
    [Test]
    public void TheFileTheApiExports_IsAccepted_ByTheLoaderThatReadsTheDatasetsLabels()
    {
        string fixture = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.evals.tests", "Labels", "Fixtures",
            "exported-label.json");
        // The fixture is a two-turn conversation (6a.4f), so the scenario has a follow-up after its question.
        using var folder = new DatasetFolder()
            .Write("scenarios/dev/sample-dev.json",
                DatasetFolder.Scenario("sample-dev", followUps: "[\"Make it six\"]")).WithManifest()
            .Write($"labels/meal-for-people.sample-dev.{SampleMode.Beta}.8f7802b9a72e.1.json",
                File.ReadAllText(fixture));

        var label = DatasetLoader.Load(folder.Root, [new MealCheck()]).Labels.Single();

        Assert.That(
            (label.Grader, label.Scenario, label.Mode, label.Run, label.Repeat, label.Turn, label.Answer, label.Pass,
                label.Note),
            Is.EqualTo(("meal-for-people", "sample-dev", SampleMode.Beta, "8f7802b9a72e", 1, 2, "Bobotie vir ses", true,
                "feeds four")));
        Assert.That(label.Turns,
            Is.EqualTo(new[]
            {
                new LabelTurn("Hello", "Bobotie vir vier"), new LabelTurn("Make it six", "Bobotie vir ses")
            }),
            "6a.4f: the whole conversation");
    }
}
