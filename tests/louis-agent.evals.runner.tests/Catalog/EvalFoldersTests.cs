using LouisAgent.Testing;
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
    public void NoEvalsSetting_ChoosesEveryEval(string? setting)
    {
        using var root = TwoIdentities();

        Assert.That(EvalFolders.Choose(Load(root), setting).Select(eval => eval.Identity),
            Is.EqualTo(new[] { SampleIdentity.Alpha, SampleIdentity.Beta }));
    }

    /// <summary>With names in EVALS=, case ignored, only the evals of those identities are chosen.</summary>
    [Test]
    public void AnEvalsSetting_ChoosesByIdentity()
    {
        using var root = TwoIdentities();

        Assert.That(EvalFolders.Choose(Load(root), SampleIdentity.Beta.ToUpperInvariant()).Select(e => e.Identity),
            Is.EqualTo(new[] { SampleIdentity.Beta }));
    }

    /// <summary>
    /// A name no eval proves is refused, naming the identities that have evals, so a typo runs nothing.
    /// </summary>
    [Test]
    public void ANameNoEvalProves_IsRefused_NamingTheOnesThatDo()
    {
        using var root = TwoIdentities();

        var refusal = Assert.Throws<RunnerException>(() => EvalFolders.Choose(Load(root), "plus"));

        Assert.That(refusal!.Message,
            Does.Contain("plus").And.Contain(SampleIdentity.Alpha).And.Contain(SampleIdentity.Beta));
    }

    /// <summary>A found folder joins the catalogue as an eval, with the identity it names, before the stub.</summary>
    [Test]
    public void AFoundFolder_JoinsTheCatalogue_WithItsIdentity()
    {
        using var root = new SampleEvalFolder("identities/sample/Evals/Greet");

        var listing = new EvalCatalog(Load(root), new StubEval()).Listing();

        Assert.That(listing.Select(line => (line.Name, line.Identity)),
            Is.EqualTo(new[] { (SampleEval.Alpha, (string?)SampleIdentity.Alpha), ("stub", null) }));
        Assert.That(listing[0].Manual, Is.EqualTo("identities/sample/Evals/Greet/manual.md"));
    }

    /// <summary>Two sample evals, of two identities.</summary>
    private static SampleEvalFolder TwoIdentities()
    {
        var root = new SampleEvalFolder("identities/alpha/Evals/Greet");
        root.AddEval("identities/beta/Evals/Greet", SampleIdentity.Beta, SampleEval.Beta);
        return root;
    }

    /// <summary>Every eval found under the root, loaded as the runner loads it.</summary>
    private static IReadOnlyList<FileEval> Load(SampleEvalFolder root) =>
    [
        .. EvalFolders.Find(root.Root)
            .Select(folder => FileEval.Load(folder.Folder, new CheckCatalog(GeneralChecks.All), folder.Source))
    ];
}
