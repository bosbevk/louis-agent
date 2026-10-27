using System.Text.RegularExpressions;
using LouisAgent.Core.Tests.RepeatedNames.Scanning;

namespace LouisAgent.Core.Tests.Categories;

// F18 step 7.6 (Louis, 2026-10-27): the tests that need something outside the test process (git, bash, a browser, Mongo, a dotnet child) are integration tests and say so, so they can run
// apart from the unit tests. A test that can skip itself because the outside is missing is the sign: it is only marked when it checks first, so this ratchet reads the sources for that check
// (an Assert.Ignore, or an Assume about a tool or an address that isn't there) and fails on any that sits outside a test or class marked [Category("Integration")]. Where the check is in a
// SetUp or a helper it covers every test of the class, so the class itself is marked.
public class IntegrationTestsAreMarkedTests
{
    private const string Marked = "Category(\"Integration\")";

    private static readonly Regex Guard = new(
        @"Assert\.Ignore\(|Assume\.That\(.*(isn't set|not installed|isn't installed|isn't here|ResolveInterpreter\(\)|FindBash\(\))", RegexOptions.Compiled);

    private static readonly Regex MethodHeader = new(@"^\s*(public|private|protected|internal)\b[^=;]*\)\s*(=>.*)?$", RegexOptions.Compiled);

    private static readonly Regex ClassHeader = new(@"^\s*(public|internal)\s+(sealed\s+|abstract\s+)*class\s", RegexOptions.Compiled);

    private static readonly Regex TestAttribute = new(@"^\s*\[(Test|TestCase|TestCaseSource|Theory)\b", RegexOptions.Compiled);

    public sealed record Unmarked(string File, int Line, string Where);

    /// <summary>The attribute lines right above the header at <paramref name="header"/> (the contiguous run of lines that start with a bracket).</summary>
    private static List<string> AttributesAbove(string[] lines, int header)
    {
        var attributes = new List<string>();
        for (int i = header - 1; i >= 0 && lines[i].TrimStart().StartsWith('['); i--) attributes.Add(lines[i]);
        return attributes;
    }

    private static int NearestAbove(string[] lines, int from, Regex pattern)
    {
        for (int i = from; i >= 0; i--)
            if (pattern.IsMatch(lines[i])) return i;
        return -1;
    }

    private static bool Has(IEnumerable<string> attributes, string text) => attributes.Any(a => a.Contains(text, StringComparison.Ordinal));

    /// <summary>
    /// Every guard in the text that is outside a marked test or class; <paramref name="file"/> only names it in the result. A guard in a test is covered by that test's mark (or the class's); one in a
    /// SetUp covers the whole class, so the class carries the mark; one in a helper covers the tests that call it, so each of them (or the class) does.
    /// </summary>
    public static IReadOnlyList<Unmarked> Scan(string file, string text)
    {
        string[] lines = text.Split('\n');
        var headers = Enumerable.Range(0, lines.Length).Where(i => MethodHeader.IsMatch(lines[i])).ToList();
        var found = new List<Unmarked>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!Guard.IsMatch(lines[i]) || lines[i].TrimStart().StartsWith("//")) continue;

            int method = headers.LastOrDefault(h => h <= i, -1);
            int type = NearestAbove(lines, i, ClassHeader);
            var methodAttributes = method < 0 ? [] : AttributesAbove(lines, method);
            bool classMarked = type >= 0 && Has(AttributesAbove(lines, type), Marked);
            if (classMarked) continue;

            if (methodAttributes.Any(a => TestAttribute.IsMatch(a)))
            {
                if (!Has(methodAttributes, Marked)) found.Add(new Unmarked(file, i + 1, "a test"));
            }
            else if (Has(methodAttributes, "SetUp"))
                found.Add(new Unmarked(file, i + 1, "a SetUp: mark the class"));
            else
                found.AddRange(UnmarkedCallers(file, lines, headers, method).Select(c => new Unmarked(file, c + 1, "a test that calls a helper which checks")));
        }
        return found;
    }

    /// <summary>The tests, in the same file, that call the helper at <paramref name="helper"/> and are not marked.</summary>
    private static IEnumerable<int> UnmarkedCallers(string file, string[] lines, List<int> headers, int helper)
    {
        if (helper < 0) yield break;
        string name = Regex.Match(lines[helper], @"(\w+)\s*(<[^>]*>)?\(").Groups[1].Value;
        var call = new Regex($@"\b{Regex.Escape(name)}\(");
        for (int k = 0; k < headers.Count; k++)
        {
            int end = k + 1 < headers.Count ? headers[k + 1] : lines.Length;
            bool calls = Enumerable.Range(headers[k], end - headers[k]).Any(l => (l != helper) && call.IsMatch(lines[l]));
            var attributes = AttributesAbove(lines, headers[k]);
            if (headers[k] != helper && calls && attributes.Any(a => TestAttribute.IsMatch(a)) && !Has(attributes, Marked)) yield return headers[k];
        }
    }

    [Test]
    public void ATestThatChecksTheOutsideIsThere_IsMarkedIntegration()
    {
        var unmarked = SourceTree.Files()
            .Select(path => (path, rel: SourceTree.Relative(path)))
            .Where(file => file.rel.StartsWith("tests/", StringComparison.Ordinal) && !file.rel.StartsWith("tests/louis-agent.core.tests/Categories/", StringComparison.Ordinal))
            .Select(file => file.path)
            .SelectMany(path => Scan(SourceTree.Relative(path), File.ReadAllText(path)))
            .Select(u => $"{u.File}:{u.Line}  {u.Where}")
            .ToList();

        Assert.That(unmarked, Is.Empty, "a test that skips itself when git, a browser, a database or an interpreter is missing is an integration test (F18 step 7.6):\n" + string.Join("\n", unmarked));
    }
}
