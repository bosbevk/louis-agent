using LouisAgent.Core.Tests.RepeatedNames.Scanning;

namespace LouisAgent.Core.Tests.RepeatedNames;

// F18 step 7.5.3c (Louis's review of 7.5.3): what the ratchet skips, it skips by the code before the literal, never by a word elsewhere on the line.
// A comment after the literal, a comment before it, or an identifier that merely ends in `Folder` must not hide a name.
public class NameScanTests
{
    private static readonly HashSet<string> Names = ["shops", "dotnet", "specialists", "web"];

    private static int Hits(string source) => NameScan.Flagged(source, Names).Count;

    [TestCase("var x = \"shops\"; // see SkillFolder")]
    [TestCase("/* the ResolveShell way */ var x = \"web\";")]
    [TestCase("var SkillFolder = \"shops\";")]
    [TestCase("var x = [\"web\"]; // evalHost = \"dotnet\"")]
    public void ANameWithAWordElsewhereOnTheLine_IsStillFlagged(string line) =>
        Assert.That(Hits(line), Is.GreaterThan(0));

    [TestCase("public const string SpecialistsFolder = \"specialists\";")]
    [TestCase("string evalHost = \"dotnet\")")]
    [TestCase("var run = ProcessRunner.Run(\"dotnet\", [\"test\"]);")]
    [TestCase("var value = element.GetProperty(\"web\");")]
    [TestCase("Assert.That(start.FileName, Is.EqualTo(\"dotnet\"));")]
    [TestCase("Assert.That(Enum.GetValues<Mode>().Select(m => m.Wire()), Is.EqualTo(new[] { \"web\", \"shops\" }));")]
    public void ANameThatIsAnExecutableAKeyOrARoleFolder_IsSkipped(string line) =>
        Assert.That(Hits(line), Is.EqualTo(0));

    // F18 step 7.5.4f: an enum file is scanned like any other file, and only the lines inside an enum's braces are skipped (that is where a name is written once).
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

    [Test]
    public void ANameInAnEnumsBody_IsSkipped_AndTheSameNameInAClassInTheSameFileIsFlagged()
    {
        var hits = NameScan.Flagged(EnumAndClass, Names);

        Assert.That(hits.Select(h => (h.Line, h.Value)), Is.EqualTo(new[] { (9, "web") }));
    }

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

        Assert.That(NameScan.Flagged(source, Names).Select(h => h.Line), Is.EqualTo(new[] { 3, 8 }),
            "no class is skipped unless it is listed");
        Assert.That(NameScan.Flagged(source, Names, skippedClasses: ["Ids"]).Select(h => h.Line),
            Is.EqualTo(new[] { 8 }));
    }
}
