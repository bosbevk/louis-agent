using LouisAgent.Choices;
using LouisAgent.Diagrams.Sources.Types;

namespace LouisAgent.DocDiagrams.Tests.Projects;

/// <summary>
/// CL65.5, Louis: "the project type and requirements must drive which diagrams is required", and "if diagrams is
/// missing add them". Every project doc says what kind of project it is about, and shows what its kind asks for
/// and what it names of its own, no less and no more.
/// </summary>
public class ProjectDocsTests
{
    /// <summary>The guide that says which pictures a project doc shows, from the repo's root.</summary>
    private const string Guide = "src/docs/guides/draw-a-diagram.md";

    /// <summary>The doc's path from the repo's root, as a person would find it.</summary>
    private static string Relative(ProjectDoc doc) =>
        System.IO.Path.GetRelativePath(TestPaths.RepoRoot, doc.Path).Replace('\\', '/');

    /// <summary>A set of types as a line of their names, in the order the enum declares them.</summary>
    private static string Names(IEnumerable<DiagramType> types) =>
        string.Join(", ", types.Order().Select(type => type.Wire()));

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

    /// <summary>
    /// Every project doc shows its overview, named for the doc, just above its <em>Purpose</em>: the picture a
    /// reader meets first.
    /// </summary>
    [Test]
    public void EveryProjectDoc_ShowsItsOverview()
    {
        var bare = ProjectDoc.All().Where(doc =>
            !doc.Pictures.Contains($"{DiagramSync.Folder}/{System.IO.Path.GetFileNameWithoutExtension(doc.Path)}.svg"));

        Assert.That(bare.Select(Relative), Is.Empty, "a project doc without its overview");
    }

    /// <summary>
    /// Every project doc shows exactly what its kind's floor and its own line ask for: none missing and none more
    /// than it names, so the doc's first lines are a true list of its pictures.
    /// </summary>
    [Test]
    public void EveryProjectDoc_ShowsWhatItsKindAsks()
    {
        var wrong = new List<string>();
        foreach (var doc in ProjectDoc.All())
        {
            var asked = ProjectFloor.Asked(doc);
            var missing = asked.Except(doc.Shown).ToList();
            var more = doc.Shown.Except(asked).ToList();

            if (missing.Count > 0)
                wrong.Add($"{Relative(doc)}: its kind asks for {Names(missing)}, which it doesn't show");
            if (more.Count > 0)
                wrong.Add($"{Relative(doc)}: shows {Names(more)}, which its kind and its own line don't name");
        }

        Assert.That(wrong, Is.Empty, "draw what is missing from the code; name in the doc's own line what it adds");
    }

    /// <summary>
    /// The guide's table of what each kind of project shows is the floor the rule holds, row for row, so a person
    /// reads the same rule the test keeps.
    /// </summary>
    [Test]
    public void TheGuidesTable_IsTheFloor()
    {
        // Each row whose first cell is a kind: the kind, and the names in its last cell.
        var table = File.ReadLines(System.IO.Path.Combine(TestPaths.RepoRoot, Guide))
            .Where(line => line.StartsWith("| `", StringComparison.Ordinal))
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries))
            .Select(cells => (Kind: KindIn(cells[1]), Shows: Names(TypesIn(cells[3]))))
            .Where(row => row.Kind is not null)
            .ToDictionary(row => row.Kind!.Value, row => row.Shows);

        Assert.Multiple(() =>
        {
            foreach (var kind in Enum.GetValues<ProjectKind>())
                Assert.That(table.GetValueOrDefault(kind), Is.EqualTo(Names(ProjectFloor.Of(kind))),
                    $"the guide's row for {kind.Wire()}");
        });
    }

    /// <summary>The kind a table's cell names between backticks, if it names one.</summary>
    private static ProjectKind? KindIn(string cell) =>
        SettingChoice.TryParse(cell.Trim('`'), out ProjectKind? kind) ? kind : null;

    /// <summary>The types a table's cell names, each between backticks.</summary>
    private static IEnumerable<DiagramType> TypesIn(string cell) => cell.Split('`').Where((_, part) => part % 2 == 1)
        .Select(name => SettingChoice.TryParse(name, out DiagramType? type) ? type : null).OfType<DiagramType>();

    /// <summary>The docs are found at all, so the rules above can't pass on none.</summary>
    [Test]
    public void TheProjectDocs_AreFound() => Assert.That(ProjectDoc.All().Count(), Is.GreaterThan(40));
}
