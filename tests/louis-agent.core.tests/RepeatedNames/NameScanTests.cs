using LouisAgent.Core.Tests.RepeatedNames.Scanning;

namespace LouisAgent.Core.Tests.RepeatedNames;

// F18 step 7.5.3c (Louis's review of 7.5.3): what the ratchet skips, it skips by the code before the literal, never by
// a word elsewhere on the line. A comment after the literal, a comment before it, or an identifier that merely ends in
// `Folder` must not hide a name.
/// <summary>
/// Feeds the repeated-names scan a few lines of C# and checks which names it flags and which it lets through.
/// </summary>
public class NameScanTests
{
    private static readonly HashSet<string> Names = ["shops", "dotnet", "specialists", "web"];

    /// <summary>How many names the scan flags in this source.</summary>
    private static int Hits(string source) => NameScan.Flagged(source, Names).Count;

    /// <summary>A skip-word in a comment, or one that only looks like a skipped name, doesn't hide a name.</summary>
    [TestCase("var x = \"shops\"; // see SkillFolder")]
    [TestCase("/* the ResolveShell way */ var x = \"web\";")]
    [TestCase("var SkillFolder = \"shops\";")]
    [TestCase("var x = [\"web\"]; // evalHost = \"dotnet\"")]
    public void ANameWithAWordElsewhereOnTheLine_IsStillFlagged(string line) =>
        Assert.That(Hits(line), Is.GreaterThan(0));

    /// <summary>
    /// An executable, a key read from JSON and a role folder are not toolsets, so they are let through.
    /// </summary>
    [TestCase("public const string SpecialistsFolder = \"specialists\";")]
    [TestCase("string evalHost = \"dotnet\")")]
    [TestCase("var run = ProcessRunner.Run(\"dotnet\", [\"test\"]);")]
    [TestCase("var value = element.GetProperty(\"web\");")]
    [TestCase("Assert.That(start.FileName, Is.EqualTo(\"dotnet\"));")]
    [TestCase("Assert.That(Enum.GetValues<Mode>().Select(m => m.Wire()), Is.EqualTo(new[] { \"web\", \"shops\" }));")]
    public void ANameThatIsAnExecutableAKeyOrARoleFolder_IsSkipped(string line) =>
        Assert.That(Hits(line), Is.EqualTo(0));

    // The formatter wraps long lines: what a name is for must not depend on where the line breaks.
    /// <summary>The same skipped names, with the line broken where Rider's formatter breaks it.</summary>
    [TestCase("var run = ProcessRunner.Run(\n    \"dotnet\",\n    [\"test\"]);")]
    [TestCase("var value = element\n    .GetProperty(\n        \"web\");")]
    [TestCase("Assert.That(start.FileName,\n    Is.EqualTo(\"dotnet\"));")]
    [TestCase(
        "Assert.That(Enum.GetValues<Mode>().Select(m => m.Wire()),\n    Is.EqualTo(new[] { \"web\", \"shops\" }));")]
    [TestCase("public const string SpecialistsFolder =\n    \"specialists\";")]
    [TestCase("string[] CommonCommands =\n[\n    \"dotnet\",\n    \"shops\",\n];")]
    [TestCase("class Runner(\n    string evalHost = \"dotnet\",\n    int repeat = 1);")]
    [TestCase("void Run(string evalHost = \"dotnet\") { }")]
    public void ASkippedName_IsSkippedWhereverTheLineBreaks(string source) =>
        Assert.That(Hits(source), Is.EqualTo(0));

    /// <summary>A name that only looks skipped is still flagged when the line is broken.</summary>
    [TestCase("var SkillFolder =\n    \"shops\";")]
    [TestCase("var x =\n    [\"web\"]; // evalHost = \"dotnet\"")]
    [TestCase("Assert.That(a,\n    Is.EqualTo(\"web\"));")]
    public void AFlaggedName_IsFlaggedWhereverTheLineBreaks(string source) =>
        Assert.That(Hits(source), Is.GreaterThan(0));

    // F18 step 7.5.4f: an enum file is scanned like any other file, and only the lines inside an enum's braces are
    // skipped (that is where a name is written once).
    private const string EnumAndClass = """
                                        public enum Mode
                                        {
                                            [WireName("web")] Web,
                                            [WireName("shops")] Shops,
                                        }

                                        public class Identity
                                        {
                                            public string Name => "web";
                                        }
                                        """;

    /// <summary>
    /// Inside an enum a name is written once, so it is skipped there and flagged anywhere else in the file.
    /// </summary>
    [Test]
    public void ANameInAnEnumsBody_IsSkipped_AndTheSameNameInAClassInTheSameFileIsFlagged()
    {
        var hits = NameScan.Flagged(EnumAndClass, Names);

        Assert.That(hits.Select(h => (h.Line, h.Value)), Is.EqualTo(new[]
        {
            (9, "web")
        }));
    }

    /// <summary>A class is skipped only when it is asked for by name.</summary>
    [Test]
    public void OnlyTheBodyOfAListedClass_IsSkipped()
    {
        const string source = """
                              public static class Ids
                              {
                                  public const string Web = "web";
                              }

                              public class Other
                              {
                                  public string Name => "web";
                              }
                              """;

        Assert.That(NameScan.Flagged(source, Names).Select(h => h.Line), Is.EqualTo(new[]
            {
                3, 8
            }),
            "no class is skipped unless it is listed");
        Assert.That(NameScan.Flagged(source, Names, skippedClasses: ["Ids"]).Select(h => h.Line),
            Is.EqualTo(new[]
            {
                8
            }));
    }
}
