using LouisAgent.Api.Services;

namespace LouisAgent.Api.Tests;

// F27 step 1 (F27-S3): the checks the portal can run, and every saved run under samples/, newest first, with its cost.
public class BenchmarkCatalogTests
{
    private string _samples = "";

    [SetUp]
    public void SetUp()
    {
        _samples = Path.Combine(Path.GetTempPath(), "louis-samples-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_samples);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_samples, recursive: true);

    private void Save(string folder, string file, string json)
    {
        string path = Path.Combine(_samples, folder, file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    [Test]
    public void TheChecks_AreTheTwoScripts()
    {
        var checks = new BenchmarkCatalog(_samples).Checks;

        Assert.That(checks.Select(c => c.Id), Is.EqualTo(new[] { "cook", "delegation" }));
        Assert.That(checks[0].Script, Is.EqualTo("samples/puls-cook-check/run_check.py"));
        Assert.That(checks[1].Script, Is.EqualTo("samples/puls-check/run_check.py"));
    }

    [Test]
    public void SavedRuns_AreListedNewestFirst_WithTheirCost()
    {
        // A repeated run has summary.json at its top (cost per mode as mean); a single run has results.json (cost per
        // mode under "all"). Either counts; a folder with neither doesn't.
        Save("sample-output-puls-cook", "summary.json", """{"delegated":{"cost":{"mean":0.0229}},"puls-web":{"cost":{"mean":0.0152}}}""");
        Save("sample-output-puls-cook", "run-1/results.json", """{"delegated":{"all":{"cost":0.0263}}}""");
        Save("sample-output-puls-check-roles", "results.json", """{"baseline":{"all":{"cost":0.0324}},"delegated":{"all":{"cost":0.0509}}}""");
        Save("sample-output-short", "README.md", "# not a check run");
        Directory.SetLastWriteTimeUtc(Path.Combine(_samples, "sample-output-puls-check-roles"), new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc));
        Directory.SetLastWriteTimeUtc(Path.Combine(_samples, "sample-output-puls-cook"), new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));

        var runs = new BenchmarkCatalog(_samples).SavedRuns();

        Assert.That(runs.Select(r => r.Folder), Is.EqualTo(new[] { "sample-output-puls-cook", "sample-output-puls-check-roles" }));
        Assert.That(runs[0].Check, Is.EqualTo("cook"));
        Assert.That(runs[0].Cost, Is.EqualTo(0.0381m).Within(0.00001m), "the modes' mean costs, added");
        Assert.That(runs[0].Repeats, Is.EqualTo(1), "one run-N folder");
        Assert.That(runs[1].Check, Is.EqualTo("delegation"));
        Assert.That(runs[1].Cost, Is.EqualTo(0.0833m).Within(0.00001m));
        Assert.That(runs[1].Repeats, Is.EqualTo(1));
    }

    [Test]
    public void NoSamplesFolder_IsAnEmptyList()
    {
        Assert.That(new BenchmarkCatalog(Path.Combine(_samples, "missing")).SavedRuns(), Is.Empty);
    }

    [Test]
    public void ARun_IsReadBack_WithItsFiles()
    {
        // The page opens a run: its summary or results, and its README, nothing else from disk.
        Save("sample-output-puls-cook", "summary.json", """{"delegated":{"cost":{"mean":0.02}}}""");
        Save("sample-output-puls-cook", "summary.md", "# 3 runs per mode");
        Save("sample-output-puls-cook", "README.md", "# Sample output");

        var run = new BenchmarkCatalog(_samples).Read("sample-output-puls-cook");

        Assert.That(run, Is.Not.Null);
        Assert.That(run!.Summary, Does.Contain("3 runs per mode"));
        Assert.That(run.Readme, Does.StartWith("# Sample output"));
        Assert.That(new BenchmarkCatalog(_samples).Read("../secrets"), Is.Null, "never outside samples/");
        Assert.That(new BenchmarkCatalog(_samples).Read("nope"), Is.Null);
    }
}
