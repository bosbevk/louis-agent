using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// The runner finds an eval in a file by its <c>eval.json</c>, anywhere under the copied eval files, and knows where
/// the folder sits in the repository from where its copy sits; <c>EVALS=</c> chooses by the identity the file names.
/// </summary>
public class EvalFoldersTests
{
    /// <summary>
    /// Every folder with an eval.json is found, with its path from the repository root; others are not.
    /// </summary>
    [Test]
    public void EveryEvalFile_IsFound_WithItsPlaceInTheRepository()
    {
        using var root = new SampleEvalFolder("identities/sample/Evals/Greet");
        root.Write("evals/other/Evals/Old/Transcripts/x.reference.txt", "");

        var found = EvalFolders.Find(root.Root);

        Assert.That(found.Select(folder => folder.Source), Is.EqualTo(new[] { "identities/sample/Evals/Greet" }));
        Assert.That(found.Single().Folder,
            Is.EqualTo(Path.Combine(root.Root, "identities", "sample", "Evals", "Greet")));
    }

    /// <summary>With EVALS= unset or empty, every eval is chosen, whoever's it is.</summary>
    [TestCase(null)]
    [TestCase("")]
    public void NoEvalsSetting_ChoosesEveryEval(string? setting) =>
        Assert.That(EvalFolders.Chosen(setting, SampleIdentity.Alpha), Is.True);

    /// <summary>With a name in EVALS=, only the evals of that identity are chosen.</summary>
    [Test]
    public void AnEvalsSetting_ChoosesByIdentity()
    {
        var puls = EvalSetKind.Puls.Wire();

        Assert.That((EvalFolders.Chosen(puls, puls), EvalFolders.Chosen(puls, SampleIdentity.Alpha)),
            Is.EqualTo((true, false)));
    }

    /// <summary>A found folder joins the catalogue as an eval, with the identity it names, before the stub.</summary>
    [Test]
    public void AFoundFolder_JoinsTheCatalogue_WithItsIdentity()
    {
        using var root = new SampleEvalFolder("identities/sample/Evals/Greet");
        var files = EvalFolders.Find(root.Root)
            .Select(folder => FileEval.Load(folder.Folder, new CheckCatalog(GeneralChecks.All), folder.Source));

        var listing = new EvalCatalog([], new StubEval(), files).Listing();

        Assert.That(listing.Select(line => (line.Name, line.Identity)),
            Is.EqualTo(new[] { (SampleEval.Alpha, (string?)SampleIdentity.Alpha), ("stub", null) }));
        Assert.That(listing[0].Manual, Is.EqualTo("identities/sample/Evals/Greet/manual.md"));
    }
}
