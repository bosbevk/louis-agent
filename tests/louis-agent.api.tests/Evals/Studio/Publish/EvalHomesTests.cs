using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Studio.Publish;

namespace LouisAgent.Api.Tests.Evals.Studio.Publish;

/// <summary>
/// Where an eval being composed lives in the workspace (FX32): an eval the runner lists is in the folder the listing
/// names, so an edit starts from it and publishes back into it; a new one has no folder yet and goes where a new eval
/// does.
/// </summary>
public class EvalHomesTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The workspace this test writes its eval folder in.</summary>
    private string _workspace = null!;

    /// <summary>A workspace of its own for every test, empty until a test makes the eval's folder.</summary>
    [SetUp]
    public void MakeAWorkspace() =>
        _workspace = Path.Combine(Path.GetTempPath(), "louis-agent-homes-" + Guid.NewGuid().ToString("N")[..8]);

    /// <summary>The workspace goes when the test is done.</summary>
    [TearDown]
    public void DeleteTheWorkspace()
    {
        if (Directory.Exists(_workspace)) Directory.Delete(_workspace, recursive: true);
    }

    /// <summary>A draft of the eval with this name.</summary>
    private static EvalDraft Draft(string name) =>
        new("d1", "weather-agent", name, $$"""{"name":"{{name}}"}""", [], "Louis", At);

    /// <summary>A catalogue that lists the weather eval in its folder named <c>Weather</c>.</summary>
    private static EvalList Catalog() => new([
        new EvalEntry("weather-eval", "Weather",
            Eval: new EvalLine("weather-eval", 1, "Weather", "weather-agent",
                Folder: "identities/louis-agent.identity.weather-agent.evals/Evals/Weather"))
    ]);

    /// <summary>An edit starts from the listed folder and publishes back into it.</summary>
    [Test]
    public void AListedEval_IsInItsListedFolder()
    {
        string listed = Path.Combine(_workspace, "identities", "louis-agent.identity.weather-agent.evals", "Evals",
            "Weather");
        Directory.CreateDirectory(listed);
        var homes = new EvalHomes(Catalog(), _workspace);

        Assert.That(homes.Existing(Draft("weather-eval")), Is.EqualTo(Path.GetFullPath(listed)));
        Assert.That(homes.TargetOf(Draft("weather-eval")).FolderName, Is.EqualTo("Weather"));
    }

    /// <summary>A new eval has no folder to start from, and goes where a new one does.</summary>
    [Test]
    public void ANewEval_HasNoFolderYet()
    {
        var homes = new EvalHomes(Catalog(), _workspace);

        Assert.That(homes.Existing(Draft("rain-eval")), Is.Null);
        Assert.That(homes.TargetOf(Draft("rain-eval")).FolderName, Is.EqualTo("RainEval"));
    }

    /// <summary>A listed eval whose folder isn't in this workspace (a host with none) has nothing to copy.</summary>
    [Test]
    public void AListedEvalWithNoFolderHere_HasNothingToStartFrom() =>
        Assert.That(new EvalHomes(Catalog(), _workspace).Existing(Draft("weather-eval")), Is.Null);
}
