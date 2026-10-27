using LouisAgent.Choices;
using LouisAgent.Core.Tests.RepeatedNames.Scanning;
using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.RepeatedNames;

// F18 step 7.5.2, the pattern everywhere (Louis, 2026-10-27: "the system should work like this everywhere"): in with SettingChoice.TryParse, out with .Wire(), the text on [WireName].
// So outside louis-agent.choices no code calls SettingChoice.Wire itself, and no enum is turned into text with ToString(), ToLowerInvariant() or a naming policy.
// The first three rules read the sources; the last reads the compiled code, where the compiler writes enum.ToString() with the enum's type, so no name needs guessing.
public class WireTextTests
{
    private static readonly string[] OutsideChoices = ["louis-agent.choices/", "tests/louis-agent.choices.tests/", "tests/louis-agent.core.tests/RepeatedNames/"];

    /// <summary>The code of each source file outside the choices project, with the comments cut off the end of a line.</summary>
    private static IEnumerable<(string File, int Line, string Code)> Code() =>
        SourceTree.Files().Select(path => (Rel: SourceTree.Relative(path), Path: path))
            .Where(f => !OutsideChoices.Any(prefix => f.Rel.StartsWith(prefix, StringComparison.Ordinal)))
            .SelectMany(f => File.ReadAllLines(f.Path).Select((text, i) => (f.Rel, i + 1, Code: Regex.Replace(text, @"//.*$", ""))));

    private static List<string> Matching(string pattern) =>
        [.. Code().Where(c => Regex.IsMatch(c.Code, pattern)).Select(c => $"{c.File}:{c.Line}  {c.Code.Trim()}")];

    [Test]
    public void NoCodeOutsideChoices_CallsSettingChoiceWireItself()
    {
        var offending = Matching(@"\bSettingChoice\s*\.\s*Wire\b");

        Assert.That(offending, Is.Empty, "write `member.Wire()` instead:\n" + string.Join("\n", offending.Take(80)) + $"\n({offending.Count} in all)");
    }

    [Test]
    public void NoNamingPolicy_TurnsAnEnumIntoText()
    {
        var offending = Matching(@"\.ConvertName\(");

        Assert.That(offending, Is.Empty, "a member's text is its [WireName]; write `member.Wire()`:\n" + string.Join("\n", offending));
    }

    [Test]
    public void NoEnum_IsCaseFoldedToBecomeText()
    {
        var offending = Matching(@"\.ToString\(\)\s*\.\s*(ToLowerInvariant|ToLower|ToUpperInvariant|ToUpper)\(\)");

        Assert.That(offending, Is.Empty, "an enum's text is `member.Wire()`:\n" + string.Join("\n", offending.Take(80)) + $"\n({offending.Count} in all)");
    }

    [Test]
    public void NoCompiledCode_CallsToStringOnAnEnumWeDeclare()
    {
        var assemblies = SourceTree.BuiltAssemblies();
        Assume.That(assemblies, Is.Not.Empty, "build the solution first: this reads the compiled projects");

        // A record's ToString (the compiler's PrintMembers) shows its members for a person and is no wire text; louis-agent.choices is the one project that may turn a member into text itself.
        var offending = EnumToStringScan.Find(assemblies.Where(a => Path.GetFileNameWithoutExtension(a) != "louis-agent.choices"))
            .Where(site => !site.Method.EndsWith("::PrintMembers", StringComparison.Ordinal))
            .Select(s => $"{s.Assembly}  {s.Method}  ({s.Enum})").Distinct().Order().ToList();

        Assert.That(offending, Is.Empty, "an enum's text is `member.Wire()`:\n" + string.Join("\n", offending.Take(120)) + $"\n({offending.Count} in all)");
    }
}
