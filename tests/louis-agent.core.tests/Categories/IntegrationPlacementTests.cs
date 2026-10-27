using LouisAgent.Core.Tests.RepeatedNames.Scanning;

namespace LouisAgent.Core.Tests.Categories;

// F18 step 7.6.3: unit and integration tests are apart (Louis, 2026-10-27). A test that leaves its process lives in `{name}.integration-tests`, and that project holds nothing else, so
// `dotnet test` on a unit project never starts a process, opens Mongo or needs a tool. The mark and the project say the same thing, and this ratchet holds them to it.
public class IntegrationPlacementTests
{
    private const string Marked = "Category(\"Integration\")";

    /// <summary>Where a test file's mark and its project disagree; a file with no test in it is left where it is (a helper belongs to whoever uses it).</summary>
    public static IReadOnlyList<string> Misplaced(string rel, string text)
    {
        string[] parts = rel.Split('/');
        if (parts.Length < 3 || parts[0] != "tests") return [];
        string project = parts[1];
        bool integrationProject = project.EndsWith(".integration-tests", StringComparison.Ordinal);
        bool unitProject = project.EndsWith(".tests", StringComparison.Ordinal);
        if (!integrationProject && !unitProject) return [];

        string[] lines = text.Split('\n');
        var found = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("public class ", StringComparison.Ordinal) && !lines[i].StartsWith("public sealed class ", StringComparison.Ordinal)) continue;
            int end = Enumerable.Range(i + 1, lines.Length - i - 1).FirstOrDefault(k => lines[k].StartsWith("public ", StringComparison.Ordinal) || lines[k].StartsWith("internal ", StringComparison.Ordinal) || lines[k].StartsWith('['), lines.Length);
            bool hasTest = Enumerable.Range(i, end - i).Any(k => System.Text.RegularExpressions.Regex.IsMatch(lines[k], @"^\s*\[(Test|TestCase|TestCaseSource)\b"));
            if (!hasTest) continue;
            bool marked = i > 0 && lines[i - 1].Contains(Marked, StringComparison.Ordinal);
            if (marked && unitProject) found.Add($"{rel}:{i + 1}  marked Integration, but in a unit project: it belongs in {project[..^".tests".Length]}.integration-tests");
            if (!marked && integrationProject) found.Add($"{rel}:{i + 1}  in an integration project, but not marked [Category(\"Integration\")] on the class");
        }
        return found;
    }

    [Test]
    public void EveryIntegrationTest_IsInAnIntegrationProject_AndNothingElseIs()
    {
        var misplaced = SourceTree.Files()
            .Select(path => (Rel: SourceTree.Relative(path), Text: File.ReadAllText(path)))
            .Where(file => !file.Rel.StartsWith("tests/louis-agent.core.tests/Categories/", StringComparison.Ordinal))
            .SelectMany(file => Misplaced(file.Rel, file.Text))
            .ToList();

        Assert.That(misplaced, Is.Empty, "unit and integration tests are apart (F18 step 7.6.3):\n" + string.Join("\n", misplaced));
    }
}
