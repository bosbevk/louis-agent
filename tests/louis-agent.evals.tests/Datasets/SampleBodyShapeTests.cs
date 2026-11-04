using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Api.Evals.Drafts.Samples;
using LouisAgent.Evals.Datasets.Samples;

namespace LouisAgent.Evals.Tests.Datasets;

/// <summary>
/// The API's <see cref="SampleBody"/> is its own copy of the harness's <see cref="SampleFile"/>, since the API never
/// carries the harness. This holds the two to one shape: every scenario file in the repo reads as a body and writes
/// back as the harness writes it, and a body the API writes, with or without an ideal answer, is a file the harness
/// reads strictly. This project references both, as it does to hold the protocol's lines to the API's reader.
/// </summary>
/// <remarks>
/// Two copies of a shape held equal by a test, as <c>ShopTableTests</c> holds two tables; here, since this project
/// already reaches both (Louis's pick).
/// </remarks>
public class SampleBodyShapeTests
{
    /// <summary>
    /// Every scenario file of every identity's evals, dev and held-out, from the repo relative to its root (the
    /// copies a build puts under bin are left out).
    /// </summary>
    private static IEnumerable<string> ScenarioFiles() =>
        Directory.EnumerateDirectories(Path.Combine(TestPaths.RepoRoot, "evals", "identities"))
            .Select(identity => Path.Combine(identity, "Evals"))
            .Where(Directory.Exists)
            .SelectMany(evals => Directory.EnumerateFiles(evals, "*.json", SearchOption.AllDirectories))
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}scenarios{Path.DirectorySeparatorChar}"))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);

    /// <summary>There are scenario files to read, so the next test can't pass on none.</summary>
    [Test]
    public void TheRepoHasScenarioFiles() => Assert.That(ScenarioFiles().Count(), Is.GreaterThan(20));

    /// <summary>
    /// Each scenario file reads as a body, and the body writes the same JSON the harness's record does.
    /// </summary>
    [TestCaseSource(nameof(ScenarioFiles))]
    public void EveryScenarioFile_ReadsAsABody_AndWritesBackAsTheHarnessWritesIt(string file)
    {
        string text = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, file));

        var body = JsonSerializer.Deserialize<SampleBody>(text, SampleBody.FileJson);
        var sample = JsonSerializer.Deserialize<SampleFile>(text, EvalJson.Strict);

        Assert.That(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(body, SampleBody.FileJson),
            JsonSerializer.SerializeToNode(sample, EvalJson.SnakeCase)), Is.True, file);
    }

    /// <summary>A body the API writes, with an ideal answer or without, is a file the harness reads strictly.</summary>
    [TestCase("Bobotie vir vier.")]
    [TestCase(null)]
    public void ABodyTheApiWrites_TheHarnessReadsStrictly(string? target)
    {
        var body = new SampleBody("cook-bobotie-four",
            [new SampleBodyMessage("user", "Gee my 'n resep vir bobotie vir vier.")],
            new SampleBodyMetadata("Malmesbury, Wes-Kaap, Suid-Afrika", "Africa/Johannesburg", ["En 'n slaai?"],
                JsonDocument.Parse("""{"routes_to":"puls-cook","serves":4}""").RootElement.Clone(), ["language:af"]),
            target);

        var sample = JsonSerializer.Deserialize<SampleFile>(JsonSerializer.Serialize(body, SampleBody.FileJson),
            EvalJson.Strict)!;

        Assert.That((sample.Id, sample.Input.Single().Content, sample.Target, sample.Metadata.Place,
                sample.Metadata.FollowUps.Single(), sample.Metadata.Expected.GetProperty("serves").GetInt32()),
            Is.EqualTo(("cook-bobotie-four", "Gee my 'n resep vir bobotie vir vier.", target,
                "Malmesbury, Wes-Kaap, Suid-Afrika", "En 'n slaai?", 4)));
    }
}
