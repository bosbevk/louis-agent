using System.IO.Compression;
using LouisAgent.Api.Evals.Studio.Publish;

namespace LouisAgent.Api.Tests.Evals.Studio.Publish;

/// <summary>
/// Where a published eval goes (F49.5): beside the identity it proves, under <c>Evals/</c>, its folder named as the
/// eval folders are (<c>puls-cook</c>'s is <c>Cook</c>… a new one's is its name in PascalCase), on a branch named
/// for both; and, where there is no workspace, the same folder as a zip.
/// </summary>
public class PublishTargetTests
{
    /// <summary>The folder and the branch come from the identity and the eval's name.</summary>
    [Test]
    public void TheFolderAndTheBranch_ComeFromTheIdentityAndTheName()
    {
        var target = PublishTarget.Of("weather-agent", "weather-eval");

        Assert.That((target.Folder, target.Branch),
            Is.EqualTo(("configured/weather-agent/evals/louis-agent.identity.weather-agent.evals/Evals/WeatherEval",
                "eval/weather-agent-weather-eval")));
    }

    /// <summary>
    /// An eval the runner already lists keeps the folder it is in (FX32): the cook's is <c>Cook</c>, not
    /// <c>PulsCook</c>, so an edit replaces it instead of making a second eval of the same name beside it.
    /// </summary>
    [Test]
    public void AnEvalTheRunnerLists_KeepsItsFolder()
    {
        var target = PublishTarget.Of("weather-agent", "weather-eval",
            "configured/weather-agent/evals/louis-agent.identity.weather-agent.evals/Evals/Weather");

        Assert.That((target.Folder, target.FolderName, target.Branch),
            Is.EqualTo(("configured/weather-agent/evals/louis-agent.identity.weather-agent.evals/Evals/Weather", "Weather",
                "eval/weather-agent-weather-eval")));
    }

    /// <summary>A listed folder outside its identity's <c>Evals/</c> isn't taken: it goes where new ones go.</summary>
    [TestCase("identities/louis-agent.identity.other-agent/Evals/Weather")]
    [TestCase("src/evals/template/Example")]
    [TestCase("configured/weather-agent/evals/louis-agent.identity.weather-agent.evals/Evals/../../escape")]
    public void AListedFolderOutsideItsIdentity_IsNotTaken(string listed) =>
        Assert.That(PublishTarget.Of("weather-agent", "weather-eval", listed).Folder,
            Is.EqualTo("configured/weather-agent/evals/louis-agent.identity.weather-agent.evals/Evals/WeatherEval"));

    /// <summary>The zip holds the folder's files under the folder's name, paths with forward slashes.</summary>
    [Test]
    public void TheZip_HoldsTheFolderUnderItsName()
    {
        string folder = Path.Combine(Path.GetTempPath(), "louis-agent-zip-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(folder, "Rubrics"));
        File.WriteAllText(Path.Combine(folder, "eval.json"), "{}");
        File.WriteAllText(Path.Combine(folder, "Rubrics", "weather-right.md"), "Is die weer reg?");
        try
        {
            using var zip = new ZipArchive(new MemoryStream(PublishTarget.Of("weather-agent", "weather-eval")
                .Zip(folder)));

            Assert.That(zip.Entries.Select(e => e.FullName),
                Is.EquivalentTo(new[] { "WeatherEval/Rubrics/weather-right.md", "WeatherEval/eval.json" }));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
