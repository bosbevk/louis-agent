using LouisAgent.Evals.Runner.Catalog;
using System.Text.Json;
using System.Text.Json.Nodes;
using LouisAgent.Evals.Runner.Studio;
using LouisAgent.Testing;

namespace LouisAgent.Evals.Runner.Tests.Studio;

/// <summary>
/// The runner's <c>--check {folder}</c>, the studio's one judge of a composed eval (F49.5, finding R): it seals a
/// manifest the studio left unhashed, loads the folder as strictly as a run would and holds it to the conformance gate,
/// then writes each problem as a line and whether it was ok as the last. A copy of the template eval is the folder.
/// </summary>
public class CheckCommandTests
{
    /// <summary>The template eval, which conforms.</summary>
    private static readonly string Template = Path.Combine(TestPaths.RepoRoot, "src", "evals", "template", "Example");

    /// <summary>The copy this test checks.</summary>
    private string _folder = null!;

    /// <summary>A fresh copy of the template for every test.</summary>
    [SetUp]
    public void CopyTheTemplate()
    {
        _folder = Path.Combine(Path.GetTempPath(), "louis-agent-check-" + Guid.NewGuid().ToString("N")[..8]);
        foreach (string file in Directory.EnumerateFiles(Template, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(_folder, Path.GetRelativePath(Template, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    /// <summary>The copy goes when the test is done.</summary>
    [TearDown]
    public void DeleteTheCopy() => Directory.Delete(_folder, recursive: true);

    /// <summary>Checks the copy and reads back its lines: the problems, and whether it was ok.</summary>
    private async Task<(bool Ok, IReadOnlyList<string> Problems)> CheckAsync(string? folder = null)
    {
        var output = new StringWriter();
        int code = await CheckCommand.RunAsync(folder ?? _folder, RunnerCatalogs.GeneralOnly, output, CancellationToken.None);
        Assert.That(code, Is.Zero, "a check that could look at the folder always ends well");

        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToList();
        var problems = lines.Where(l => l.TryGetProperty("problem", out _))
            .Select(l => l.GetProperty("problem").GetString()!).ToList();
        return (lines[^1].GetProperty("ok").GetBoolean(), problems);
    }

    /// <summary>The template is ok, with no problems.</summary>
    [Test]
    public async Task AConformingFolder_IsOk() =>
        Assert.That(await CheckAsync(), Is.EqualTo((true, (IReadOnlyList<string>)[])));

    /// <summary>
    /// A manifest the studio left without its hash is sealed with the hash of the scenarios, then checked: the
    /// template's own hash comes back.
    /// </summary>
    [Test]
    public async Task AnUnhashedManifest_IsSealed_ThenChecked()
    {
        string manifest = Path.Combine(_folder, "Dataset", "dataset.json");
        var node = JsonNode.Parse(File.ReadAllText(manifest))!.AsObject();
        string hash = node["content_hash"]!.GetValue<string>();
        node["content_hash"] = "";
        File.WriteAllText(manifest, node.ToJsonString());

        var (ok, problems) = await CheckAsync();

        Assert.That((ok, problems), Is.EqualTo((true, (IReadOnlyList<string>)[])));
        Assert.That(JsonNode.Parse(File.ReadAllText(manifest))!["content_hash"]!.GetValue<string>(), Is.EqualTo(hash));
    }

    /// <summary>A check type the catalogue doesn't have is a problem, naming it, as a run would refuse it.</summary>
    [Test]
    public async Task AnUnknownCheckType_IsAProblem_NamingIt()
    {
        string file = Path.Combine(_folder, "eval.json");
        File.WriteAllText(file, File.ReadAllText(file).Replace("\"within-time\"", "\"within-tme\""));

        var (ok, problems) = await CheckAsync();

        Assert.That(ok, Is.False);
        Assert.That(problems.Single(), Does.Contain("within-tme"));
    }

    /// <summary>A scenario with no reference transcript fails the conformance gate.</summary>
    [Test]
    public async Task AScenarioWithNoReference_IsAProblem()
    {
        Directory.Delete(Path.Combine(_folder, "Transcripts"), recursive: true);

        var (ok, problems) = await CheckAsync();

        Assert.That(ok, Is.False);
        Assert.That(problems, Is.Not.Empty);
    }

    /// <summary>A folder that isn't there is one problem, saying where it looked.</summary>
    [Test]
    public async Task AFolderThatIsNotThere_IsAProblem() =>
        Assert.That((await CheckAsync(_folder + "-nope")).Problems.Single(), Does.Contain("-nope"));
}
