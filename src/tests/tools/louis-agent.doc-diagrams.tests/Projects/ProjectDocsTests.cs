namespace LouisAgent.DocDiagrams.Tests.Projects;

/// <summary>
/// CL65.5, Louis: "the project type and requirements must drive which diagrams is required". So every project
/// doc first says what kind of project it is about, from a fixed list.
/// </summary>
public class ProjectDocsTests
{
    /// <summary>The doc's path from the repo's root, as a person would find it.</summary>
    private static string Relative(ProjectDoc doc) =>
        System.IO.Path.GetRelativePath(TestPaths.RepoRoot, doc.Path).Replace('\\', '/');

    /// <summary>
    /// Every project doc opens with a kind line whose first words are a kind of the list, so a rule can ask what
    /// the doc must show.
    /// </summary>
    [Test]
    public void EveryProjectDoc_StatesItsKind()
    {
        var unstated = ProjectDoc.All().Where(doc => doc.Kind is null).Select(Relative).Order(StringComparer.Ordinal);

        Assert.That(unstated, Is.Empty, "a project doc whose kind line names no kind of the list");
    }

    /// <summary>The docs are found at all, so the rule above can't pass on none.</summary>
    [Test]
    public void TheProjectDocs_AreFound() => Assert.That(ProjectDoc.All().Count(), Is.GreaterThan(40));
}
