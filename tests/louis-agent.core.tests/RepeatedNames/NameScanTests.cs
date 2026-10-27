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
    public void ANameWithAWordElsewhereOnTheLine_IsStillFlagged(string line) => Assert.That(Hits(line), Is.GreaterThan(0));

    [TestCase("public const string SpecialistsFolder = \"specialists\";")]
    [TestCase("string evalHost = \"dotnet\")")]
    [TestCase("var run = ProcessRunner.Run(\"dotnet\", [\"test\"]);")]
    [TestCase("var value = element.GetProperty(\"web\");")]
    [TestCase("Assert.That(start.FileName, Is.EqualTo(\"dotnet\"));")]
    public void ANameThatIsAnExecutableAKeyOrARoleFolder_IsSkipped(string line) => Assert.That(Hits(line), Is.EqualTo(0));
}
