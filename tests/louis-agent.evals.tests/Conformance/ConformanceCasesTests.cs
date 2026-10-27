using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Tests.Datasets;

namespace LouisAgent.Evals.Tests.Conformance;

// The conformance cases kept beside an eval (F18 step 4.2b review): read from its files, by scenario. Written first: the loader threw
// NotImplementedException on the red run.
public class ConformanceCasesTests
{
    private static readonly string[] Conversation =
    [
        SampleMode.Fill(
            "@{\"event\": \"mode\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"toolsets\": [\"") +
        SampleToolset.Gamma + "\", \"" + SampleToolset.Alpha +
        "\"], \"role\": \"puls/voice-af\", \"llm\": null, \"conversations\": 1}",
        SampleMode.Fill(
            "@{\"event\": \"sse\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"s\", \"turn\": 1, \"type\": \"content_block_delta\", \"data\": {\"type\": \"content_block_delta\", \"index\": 0, \"delta\": {\"type\": \"text_delta\", \"text\": \"Hallo\"}}}"),
        SampleMode.Fill(
            "@{\"event\": \"turn\", \"t\": 1.0, \"mode\": \"<ModeBeta>\", \"repeat\": 1, \"task\": \"s\", \"turn\": 1, \"session\": \"sess_1\", \"question\": \"Hallo\", \"answer\": \"Hallo\", \"role\": null, \"seconds\": 0.5, \"error\": null}"),
    ];

    [Test]
    public void LoadsTheReferenceAndEachKnownBadBeside()
    {
        using var folder = new DatasetFolder()
            .Write("s.reference.txt", string.Join("\n", Conversation))
            .Write("s.bad.slow.txt", string.Join("\n", Conversation))
            .Write("s.bad.error.txt", string.Join("\n", Conversation));

        var only = ConformanceCases.Load(folder.Root, [Scenario("s")]).Single();

        Assert.That(only.Scenario, Is.EqualTo("s"));
        Assert.That((only.Mode.Name, only.Mode.Role), Is.EqualTo((SampleMode.Beta, "puls/voice-af")));
        Assert.That(only.Mode.Toolsets, Is.EqualTo(new[] { SampleToolset.Gamma, SampleToolset.Alpha }));
        Assert.That(only.Reference.Turns.Single().Question, Is.EqualTo("Hallo"));
        Assert.That(only.KnownBad.Select(k => k.Label), Is.EqualTo(new[] { "error", "slow" }));
    }

    [Test]
    public void RefusesAKnownBadWithNoReference()
    {
        using var folder = new DatasetFolder().Write("s.bad.error.txt", string.Join("\n", Conversation));

        Assert.That(() => ConformanceCases.Load(folder.Root, [Scenario("s")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("s.bad.error.txt"));
    }

    [Test]
    public void RefusesAFileOfAScenarioThatIsNotInTheEval()
    {
        // The scenario was deleted from the eval, and its files were not: they are stray, and the loader says which.
        using var folder = new DatasetFolder()
            .Write("s.reference.txt", string.Join("\n", Conversation))
            .Write("t.bad.error.txt", string.Join("\n", Conversation));

        Assert.That(() => ConformanceCases.Load(folder.Root, [Scenario("s")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("t.bad.error.txt"));
    }

    [Test]
    public void RefusesAMisspelledReference()
    {
        using var folder = new DatasetFolder()
            .Write("s.reference.txt", string.Join("\n", Conversation))
            .Write("s.refrence.txt", string.Join("\n", Conversation));

        Assert.That(() => ConformanceCases.Load(folder.Root, [Scenario("s")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("s.refrence.txt"));
    }

    private static EvalScenario Scenario(string id) =>
        new(id, new EvalInput(null, "Etc/UTC", ["Hallo"]), JsonDocument.Parse("{}").RootElement.Clone(), [],
            DatasetSplit.Dev);
}
