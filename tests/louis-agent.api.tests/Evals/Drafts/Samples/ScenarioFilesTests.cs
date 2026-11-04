using LouisAgent.Api.Evals.Drafts.Samples;

namespace LouisAgent.Api.Tests.Evals.Drafts.Samples;

/// <summary>
/// A sample is made from the scenario its conversation ran, read from the eval's folder in the workspace: dev first,
/// then held-out. Nothing is read from outside the eval folders (<c>evals/</c>, <c>identities/</c>, <c>packs/</c>), and
/// a scenario's name can't climb out of its own folder; what can't be read is simply not there.
/// </summary>
public class ScenarioFilesTests
{
    /// <summary>The repo's root, five folders up from where the tests run.</summary>
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    /// <summary>The cook eval's folder, as the runner's listing names it.</summary>
    private const string Cook = "evals/identities/louis-agent.identity.puls.evals/Evals/Cook";

    /// <summary>
    /// A dev scenario is read whole: its question, place, follow-ups, expected values and categories.
    /// </summary>
    [Test]
    public void ADevScenario_IsReadWhole()
    {
        var scenario = ScenarioFiles.Read(RepoRoot, Cook, "cook-af")!;

        Assert.That((scenario.Id, scenario.Metadata.Place, scenario.Metadata.FollowUps.Single()),
            Is.EqualTo(("cook-af", "Stellenbosch, Wes-Kaap, Suid-Afrika",
                "Maak dit eerder vir ses mense. Wat kos dit nou?")));
        Assert.That(scenario.Input.Single().Content, Does.StartWith("Wat kan ek vanaand vir vier mense kook"));
        Assert.That(scenario.Metadata.Expected.GetProperty("budget").GetInt32(), Is.EqualTo(250));
        Assert.That(scenario.Metadata.Categories, Is.EqualTo(new[] { "language:af", "budget", "follow-up" }));
    }

    /// <summary>A held-out scenario is found too.</summary>
    [Test]
    public void AHeldOutScenario_IsFoundToo() =>
        Assert.That(ScenarioFiles.Read(RepoRoot, Cook, "cook-paarl")?.Metadata.Place,
            Is.EqualTo("Paarl, Wes-Kaap, Suid-Afrika"));

    /// <summary>A scenario the eval doesn't have, and an eval with no folder, give none.</summary>
    [Test]
    public void AScenarioThatIsNotThere_OrAnEvalWithNoFolder_GivesNone()
    {
        Assert.That(ScenarioFiles.Read(RepoRoot, Cook, "cook-atlantis"), Is.Null);
        Assert.That(ScenarioFiles.Read(RepoRoot, null, "cook-af"), Is.Null);
    }

    /// <summary>A folder outside the eval folders, even by climbing out of one, is never read.</summary>
    [TestCase("docs")]
    [TestCase("identities/../docs")]
    [TestCase("../outside")]
    public void AFolderOutsideTheEvalFolders_IsNeverRead(string folder) =>
        Assert.That(ScenarioFiles.Read(RepoRoot, folder, "cook-af"), Is.Null, folder);

    /// <summary>A scenario name with a path in it is never read, so it can't climb out of the scenarios.</summary>
    [TestCase("../../eval")]
    [TestCase("dev/cook-af")]
    [TestCase("")]
    public void AScenarioNameWithAPathInIt_IsNeverRead(string scenario) =>
        Assert.That(ScenarioFiles.Read(RepoRoot, Cook, scenario), Is.Null, scenario);
}
