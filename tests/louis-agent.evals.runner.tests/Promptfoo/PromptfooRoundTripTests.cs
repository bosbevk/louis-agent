using System.Text.Json;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Promptfoo;
using LouisAgent.Evals.Runner.Studio;
using YamlDotNet.RepresentationModel;

namespace LouisAgent.Evals.Runner.Tests.Promptfoo;

/// <summary>
/// promptfoo in and out (F49.5, finding T): a promptfoo file becomes an eval folder the runner loads (its tests the
/// scenarios, the shared asserts the eval's checks by the mapping table, a test's own <c>contains</c> an expected
/// value, an <c>llm-rubric</c> a judge with its rubric file), what has no counterpart named rather than dropped; and
/// the folder goes back to a promptfoo file with the same questions and asserts.
/// </summary>
public class PromptfooRoundTripTests
{
    /// <summary>A promptfoo file with shared asserts, two tests, and one assert we have no counterpart for.</summary>
    private const string File = """
                                description: Cook answers in Afrikaans
                                prompts:
                                  - "{{question}}"
                                defaultTest:
                                  assert:
                                    - type: latency
                                      threshold: 120000
                                    - type: not-contains
                                      value: "I'm sorry"
                                    - type: llm-rubric
                                      value: Is this a meal for four people?
                                tests:
                                  - description: Bobotie for four
                                    vars:
                                      question: Wat kan ek vir vier kook?
                                    assert:
                                      - type: contains
                                        value: bobotie
                                  - vars:
                                      question: En vir ses?
                                    assert:
                                      - type: icontains
                                        value: ses
                                      - type: javascript
                                        value: output.length > 10
                                """;

    /// <summary>The folder this test writes.</summary>
    private string _folder = null!;

    /// <summary>A fresh folder for every test.</summary>
    [SetUp]
    public void MakeAFolder() =>
        _folder = Path.Combine(Path.GetTempPath(), "louis-agent-promptfoo-" + Guid.NewGuid().ToString("N")[..8]);

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void DeleteTheFolder()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    /// <summary>The file becomes an eval the runner loads: its checks, its scenarios, its rubric.</summary>
    [Test]
    public void TheFile_BecomesAnEvalTheRunnerLoads()
    {
        var skipped = PromptfooImport.Write(File, _folder);
        ManifestSeal.Seal(_folder);

        var eval = FileEval.Load(_folder, RunnerChecks.Catalog, RunnerReaders.Catalog, "studio");

        Assert.That(eval.Name, Is.EqualTo("cook-answers-in-afrikaans"));
        Assert.That(eval.Checks.Select(c => c.Name),
            Is.EquivalentTo(new[] { "within-time", "not-contains", "judge", "mentions-expected" }));
        Assert.That(eval.Scenarios.Select(s => (s.Id, s.Expected.GetProperty("contains").GetString())),
            Is.EqualTo(new[] { ("bobotie-for-four", "bobotie"), ("test-2", "ses") }));
        Assert.That(System.IO.File.ReadAllText(Path.Combine(_folder, "Rubrics", "rubric-1.md")),
            Is.EqualTo("Is this a meal for four people?"));
        Assert.That(skipped.Single(), Does.Contain("javascript"));
    }

    /// <summary>The settings follow the table: a latency in ms is a time in seconds, a value a list of texts.</summary>
    [Test]
    public void TheSettings_FollowTheTable()
    {
        PromptfooImport.Write(File, _folder);

        var checks = JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(_folder, "eval.json"))).RootElement
            .GetProperty("checks").EnumerateArray().ToDictionary(c => c.GetProperty("type").GetString()!);

        Assert.That(checks["within-time"].GetProperty("parameters").GetProperty("seconds").GetInt32(), Is.EqualTo(120));
        Assert.That(checks["not-contains"].GetProperty("parameters").GetProperty("texts")[0].GetString(),
            Is.EqualTo("I'm sorry"));
        Assert.That(checks["judge"].GetProperty("parameters").GetProperty("rubric").GetString(),
            Is.EqualTo("Rubrics/rubric-1.md"));
    }

    /// <summary>The folder goes back to promptfoo with the same questions and asserts.</summary>
    [Test]
    public void TheFolder_GoesBackToTheSameQuestionsAndAsserts()
    {
        PromptfooImport.Write(File, _folder);

        var yaml = new YamlStream();
        yaml.Load(new StringReader(PromptfooExport.Yaml(_folder)));
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;

        var shared = ((YamlSequenceNode)((YamlMappingNode)root["defaultTest"])["assert"])
            .Cast<YamlMappingNode>().Select(a => ((YamlScalarNode)a["type"]).Value).ToList();
        Assert.That(shared, Is.EquivalentTo(new[] { "latency", "not-contains", "llm-rubric" }));
        var tests = ((YamlSequenceNode)root["tests"]).Cast<YamlMappingNode>().ToList();
        Assert.That(tests.Select(t => ((YamlScalarNode)((YamlMappingNode)t["vars"])["question"]).Value),
            Is.EqualTo(new[] { "Wat kan ek vir vier kook?", "En vir ses?" }));
        Assert.That(tests.Select(t => ((YamlScalarNode)((YamlMappingNode)((YamlSequenceNode)t["assert"])[0])["value"])
            .Value), Is.EqualTo(new[] { "bobotie", "ses" }));
    }
}
