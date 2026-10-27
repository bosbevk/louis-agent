using System.Text.RegularExpressions;
using LouisAgent.Core.Tests.RepeatedNames.Scanning;
using LouisAgent.Testing;

namespace LouisAgent.Core.Tests.Categories;

// F18 step 7.6 (Louis, 2026-10-27): the tests that need something outside the test process (git, bash, a browser, Mongo, a dotnet child) are integration tests and say so, so they can run
// apart from the unit tests. A test that can skip itself because the outside is missing is the sign: it is only marked when it checks first, so this ratchet reads the sources for that check
// (an Assert.Ignore, or an Assume about a tool or an address that isn't there) and fails on any that sits outside a test or class marked [Category("Integration")]. Where the check is in a
// SetUp or a helper it covers every test of the class, so the class itself is marked.
public class IntegrationTestsAreMarkedTests
{
    private const string Marked = "Category(\"Integration\")";

    /// <summary>The old way to ask for the outside: a raw skip. It is banned in the tests now (<see cref="Outside"/> is the one way), but still read as a guard so a stray one is also seen as unmarked.</summary>
    public static readonly Regex RawGuard = new(
        @"Assert\.Ignore\(|Assume\.That\(.*(isn't set|not installed|isn't installed|isn't here|ResolveInterpreter\(\)|FindBash\(\))", RegexOptions.Compiled);

    private static readonly Regex Guard = new(@"Outside\.(Tool|Stack)\(|" + RawGuard, RegexOptions.Compiled);

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

    /// <summary>What takes a test out of its process in the code itself: a real process, Mongo, the stand-in build or the runner's built dll.</summary>
    private const string UseMarkers = @"\bnew\s+ProcessStartInfo\(|\bProcessStartInfo\(|\bProcess\.Start\(|\bMongoClient\b|\bStandInBuild\b|\bRunnerBuild\b";

    private static readonly Regex TopLevelClass = new(@"^(public|internal)\s+(sealed\s+|abstract\s+|static\s+)*class\s", RegexOptions.Compiled);

    /// <summary>
    /// The second signal (Louis, 2026-10-27: "both signals"): a test class that uses a real process, Mongo, the stand-in build or the runner's built dll, or calls a helper type that does
    /// (<paramref name="helperTypes"/>), leaves the test process whether or not it checks first, so the class itself carries the mark. Comments and string literals are blanked first: a word in one is not a use.
    /// </summary>
    public static IReadOnlyList<Unmarked> ScanUses(string file, string text, IReadOnlyCollection<string> helperTypes)
    {
        string[] lines = text.Split('\n');
        string[] code = CodeOnly.Of(text).Split('\n');
        string pattern = helperTypes.Count == 0 ? UseMarkers : UseMarkers + @"|\b(" + string.Join("|", helperTypes.Select(Regex.Escape)) + @")\b";
        var uses = new Regex(pattern);
        var starts = Enumerable.Range(0, lines.Length).Where(i => TopLevelClass.IsMatch(lines[i])).ToList();
        var found = new List<Unmarked>();
        for (int k = 0; k < starts.Count; k++)
        {
            int end = k + 1 < starts.Count ? starts[k + 1] : lines.Length;
            var body = Enumerable.Range(starts[k], end - starts[k]).ToList();
            bool hasTest = body.Any(l => TestAttribute.IsMatch(lines[l]));
            bool marked = Has(AttributesAbove(lines, starts[k]), Marked);
            if (hasTest && !marked && body.Any(l => uses.IsMatch(code[l]))) found.Add(new Unmarked(file, starts[k] + 1, "a test class that uses a process, Mongo or the runner: mark the class"));
        }
        return found;
    }

    /// <summary>The helper types of louis-agent.testing that start a process themselves: a class that calls one leaves the test process too.</summary>
    public static IReadOnlyCollection<string> HelperTypes(IEnumerable<(string Rel, string Text)> files)
    {
        var uses = new Regex(UseMarkers);
        return [.. files.Where(f => f.Rel.StartsWith("tests/louis-agent.testing/", StringComparison.Ordinal) && uses.IsMatch(CodeOnly.Of(f.Text)))
            .SelectMany(f => Regex.Matches(f.Text, @"\b(?:class|record|struct)\s+(\w+)").Select(m => m.Groups[1].Value)).Distinct()];
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

    [Test]
    public void ATestClassThatLeavesItsProcess_IsMarkedIntegration()
    {
        var files = SourceTree.Files()
            .Select(path => (Rel: SourceTree.Relative(path), Text: File.ReadAllText(path)))
            .Where(file => file.Rel.StartsWith("tests/", StringComparison.Ordinal) && !file.Rel.StartsWith("tests/louis-agent.core.tests/Categories/", StringComparison.Ordinal))
            .ToList();
        var helpers = HelperTypes(files);

        var unmarked = files.SelectMany(file => ScanUses(file.Rel, file.Text, helpers)).Select(u => $"{u.File}:{u.Line}  {u.Where}").ToList();

        Assert.That(unmarked, Is.Empty, "a test class that starts a process, opens Mongo or runs the stand-in or the runner is an integration test (F18 step 7.6):\n" + string.Join("\n", unmarked));
    }

    [Test]
    public void TheOutsideIsAskedForThroughOutside_NeverWithARawSkip()
    {
        var raw = SourceTree.Files()
            .Select(path => (path, rel: SourceTree.Relative(path)))
            .Where(file => file.rel.StartsWith("tests/", StringComparison.Ordinal) && !file.rel.StartsWith("tests/louis-agent.core.tests/Categories/", StringComparison.Ordinal) && !file.rel.EndsWith("/Outside.cs", StringComparison.Ordinal))
            .SelectMany(file => File.ReadAllLines(file.path).Select((line, i) => (file.rel, line, number: i + 1)))
            .Where(l => RawGuard.IsMatch(l.line) && !l.line.TrimStart().StartsWith("//"))
            .Select(l => $"{l.rel}:{l.number}  {l.line.Trim()}")
            .ToList();

        Assert.That(raw, Is.Empty, "a test asks for git, a tool or an address with Outside.Tool or Outside.Stack, so that a missing tool fails where INTEGRATION_REQUIRED is set (F18 step 7.6):\n" + string.Join("\n", raw));
    }
}
