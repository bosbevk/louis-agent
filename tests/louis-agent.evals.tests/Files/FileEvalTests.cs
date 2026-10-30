using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Tests.Datasets;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Files;

/// <summary>
/// An eval that is a folder of files: <c>eval.json</c>, its dataset, a rubric and a manual, built into an eval the
/// harness runs, with no C# of its own.
/// </summary>
public class FileEvalTests
{
    /// <summary>Where the folder sits in the repository, as the runner will say it.</summary>
    private const string Source = "evals/sample";

    /// <summary>
    /// A sample eval: one mode that can hand on, four checks (a general one renamed, a route, a judge and a person's
    /// review of it), and a manual.
    /// </summary>
    private const string EvalFile =
        $$"""
          {
            "schema_version": 1,
            "name": "{{SampleEval.Alpha}}",
            "identity": "{{SampleIdentity.Alpha}}",
            "version": 3,
            "proves": "The agent hands a question on where it should, and answers warmly.",
            "manual": "manual.md",
            "modes": [ { "name": "{{SampleMode.Alpha}}", "role": "{{SampleRole.Alpha}}", "toolsets": ["{{SampleToolset.Alpha}}"] } ],
            "checks": [
              { "type": "no-error", "name": "sample-no-error" },
              { "type": "routed-to", "parameters": { "expected_key": "routes_to", "delegating_toolsets": ["{{SampleToolset.Alpha}}"] } },
              { "type": "judge", "parameters": { "rubric": "rubrics/warm.md", "ask": ["tone"] } },
              { "type": "human", "parameters": { "reviews": "judge" } }
            ]
          }
          """;

    /// <summary>
    /// The folder's eval: its name, version, modes, scenarios, dataset and checks in the file's order.
    /// </summary>
    [Test]
    public async Task AFolder_IsAnEval()
    {
        using var folder = SampleFolder(EvalFile);

        var eval = FileEval.Load(folder.Root, Catalog, Source);
        var modes = await eval.ModesAsync(null!, CancellationToken.None);

        Assert.That((eval.Name, eval.Identity, eval.Version), Is.EqualTo((SampleEval.Alpha, SampleIdentity.Alpha, 3)));
        Assert.That(modes.Select(mode => (mode.Name, mode.Role, mode.Toolsets.Single())),
            Is.EqualTo(new[] { (SampleMode.Alpha, SampleRole.Alpha, SampleToolset.Alpha) }));
        Assert.That(eval.Scenarios.Select(scenario => scenario.Id), Is.EqualTo(new[] { "warm-hello" }));
        Assert.That(eval.Dataset?.Name, Is.EqualTo("sample-dataset"));
        Assert.That(eval.Checks.Select(check => check.Name),
            Is.EqualTo(new[] { "sample-no-error", "routed-to", "judge", "judge.human" }));
        Assert.That(eval.Manual, Is.EqualTo("evals/sample/manual.md"));
    }

    /// <summary>
    /// A check the file renames grades under the name the file gives it, so a stored run reads the same.
    /// </summary>
    [Test]
    public async Task ARenamedCheck_GradesUnderItsName()
    {
        using var folder = SampleFolder(EvalFile);

        var grade = await FileEval.Load(folder.Root, Catalog, Source).Checks[0]
            .GradeAsync(Trial(Turn(1, [Text("Hallo")])), CancellationToken.None);

        Assert.That(grade.Check, Is.EqualTo("sample-no-error"));
    }

    /// <summary>A check type the catalogue doesn't have is refused, naming the file and the type.</summary>
    [Test]
    public void AnUnknownCheckType_IsRefused_NamingTheFileAndTheType()
    {
        using var folder = SampleFolder(EvalFile.Replace("\"type\": \"no-error\"", "\"type\": \"no-eror\""));

        var refusal = Assert.Throws<InvalidDataException>(() => FileEval.Load(folder.Root, Catalog, Source));

        Assert.That(refusal!.Message, Does.Contain("eval.json").And.Contain("no-eror"));
    }

    /// <summary>A file with no manual gives the portal none to show.</summary>
    [Test]
    public void NoManual_IsNone()
    {
        using var folder = SampleFolder(EvalFile.Replace("\"manual\": \"manual.md\",", ""));

        Assert.That(FileEval.Load(folder.Root, Catalog, Source).Manual, Is.Null);
    }

    /// <summary>
    /// A turn's facts are read from its events, for any eval: where it went, the calls made, the hosts a URL named and
    /// the error.
    /// </summary>
    [Test]
    public void ATurnsFacts_AreReadFromItsEvents()
    {
        using var folder = SampleFolder(EvalFile);
        var turn = Turn(1,
        [
            Call(SampleRole.Alpha), ToolCall("ReadPage", """{"url": "https://www.example.com/a"}"""), Error()
        ]);

        var facts = ((IEval)FileEval.Load(folder.Root, Catalog, Source)).TurnFactsOf(turn, Mode);

        Assert.That((facts.Role, facts.AgentCalls, facts.SpecialistCalls, facts.Error),
            Is.EqualTo((SampleRole.Alpha, 2, 0, "overloaded")));
        Assert.That(facts.Hosts, Is.EqualTo(new[] { "example.com" }));
    }

    /// <summary>The catalogue of the general checks, as the runner will gather it.</summary>
    private static readonly CheckCatalog Catalog = new(GeneralChecks.All);

    /// <summary>A sample eval folder: this <c>eval.json</c>, a one-scenario dataset, a rubric and a manual.</summary>
    private static DatasetFolder SampleFolder(string evalFile)
    {
        var folder = new DatasetFolder()
            .Write("eval.json", evalFile)
            .Write("manual.md", "# Sample\n")
            .Write("rubrics/warm.md", "# Judge: is the answer warm\n\nSay whether the answer is warm.\n")
            .Write("Dataset/scenarios/dev/warm-hello.json",
                DatasetFolder.Scenario("warm-hello", """{"routes_to": null, "tone": "warm"}"""));
        var hash = DatasetHash.Of(Path.Combine(folder.Root, "Dataset", "scenarios"));
        return folder.Write("Dataset/dataset.json",
            "{\"name\": \"sample-dataset\", \"version\": 1, \"covers\": \"A sample task\", \"content_hash\": \"" + hash
            + "\", \"changes\": [\"2026-10-30: a sample change\"]}");
    }
}
