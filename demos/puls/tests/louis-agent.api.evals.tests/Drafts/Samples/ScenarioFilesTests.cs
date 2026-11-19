using LouisAgent.Api.Evals.Drafts.Samples;

namespace LouisAgent.Api.Evals.Tests.Evals.Drafts.Samples;

/// <summary>
/// A sample is made from the scenario its conversation ran, read from the eval's folder in the workspace: dev first,
/// then held-out. Nothing is read from outside the eval folders (<c>evals/</c>, <c>identities/</c>, <c>packs/</c>), and
/// a scenario's name can't climb out of its own folder; what can't be read is simply not there. The eval is the
/// tests' own, in a workspace of its own (F166.1).
/// </summary>
public class ScenarioFilesTests
{
    /// <summary>The workspace with the sample eval's scenarios in it, written once for these tests.</summary>
    private static readonly SampleEvalWorkspace Workspace = SampleEvalWorkspace.Create();

    /// <summary>The sample eval's folder, as the runner's listing names it.</summary>
    private const string Folder = SampleEvalWorkspace.Folder;

    /// <summary>
    /// A dev scenario is read whole: its question, place, follow-ups, expected values and categories.
    /// </summary>
    [Test]
    public void ADevScenario_IsReadWhole()
    {
        var scenario = ScenarioFiles.Read(Workspace.Root, Folder, SampleEvalWorkspace.Dinner)!;

        Assert.That((scenario.Id, scenario.Metadata.Place, scenario.Metadata.FollowUps.Single()),
            Is.EqualTo((SampleEvalWorkspace.Dinner, SampleEvalWorkspace.Place, SampleEvalWorkspace.FollowUp)));
        Assert.That(scenario.Input.Single().Content, Is.EqualTo(SampleEvalWorkspace.Question));
        Assert.That(scenario.Metadata.Expected.GetProperty("budget").GetInt32(),
            Is.EqualTo(SampleEvalWorkspace.Budget));
        Assert.That(scenario.Metadata.Categories, Is.EqualTo(SampleEvalWorkspace.Categories));
    }

    /// <summary>A held-out scenario is found too.</summary>
    [Test]
    public void AHeldOutScenario_IsFoundToo() =>
        Assert.That(ScenarioFiles.Read(Workspace.Root, Folder, SampleEvalWorkspace.HeldOutDinner)?.Metadata.Place,
            Is.EqualTo(SampleEvalWorkspace.HeldOutPlace));

    /// <summary>A scenario the eval doesn't have, and an eval with no folder, give none.</summary>
    [Test]
    public void AScenarioThatIsNotThere_OrAnEvalWithNoFolder_GivesNone()
    {
        Assert.That(ScenarioFiles.Read(Workspace.Root, Folder, SampleEvalWorkspace.Missing), Is.Null);
        Assert.That(ScenarioFiles.Read(Workspace.Root, null, SampleEvalWorkspace.Dinner), Is.Null);
    }

    /// <summary>A folder outside the eval folders, even by climbing out of one, is never read.</summary>
    [TestCase("docs")]
    [TestCase("identities/../docs")]
    [TestCase("../outside")]
    public void AFolderOutsideTheEvalFolders_IsNeverRead(string folder) =>
        Assert.That(ScenarioFiles.Read(Workspace.Root, folder, SampleEvalWorkspace.Dinner), Is.Null, folder);

    /// <summary>A scenario name with a path in it is never read, so it can't climb out of the scenarios.</summary>
    [TestCase("../../eval")]
    [TestCase("dev/dinner")]
    [TestCase("")]
    public void AScenarioNameWithAPathInIt_IsNeverRead(string scenario) =>
        Assert.That(ScenarioFiles.Read(Workspace.Root, Folder, scenario), Is.Null, scenario);
}
