namespace LouisAgent.Core.Tests.Categories;

// The scan of IntegrationTestsAreMarkedTests, fed small texts: a check that the outside is there sits in a test, a SetUp or a helper, and each has its own rule about what must carry the mark.
public class IntegrationScanTests
{
    private static string Class(string body, string attributes = "") => $"{attributes}\npublic class Sample\n{{\n{body}\n}}";

    [Test]
    public void ACheckInAnUnmarkedTest_IsFlagged()
    {
        string text = Class("    [Test]\n    public void Reads()\n    {\n        Assume.That(url, Is.Not.Null, \"BROWSER_URL isn't set\");\n    }");

        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", text).Select(u => u.Where), Is.EqualTo(new[] { "a test" }));
    }

    [Test]
    public void ACheckInAMarkedTest_OrInAMarkedClass_IsNotFlagged()
    {
        string marked = Class("    [Test, Category(\"Integration\")]\n    public void Reads()\n    {\n        Assert.Ignore(\"git isn't installed\");\n    }");
        string inClass = Class("    [Test]\n    public void Reads()\n    {\n        Assert.Ignore(\"git isn't installed\");\n    }", "[Category(\"Integration\")]");

        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", marked), Is.Empty);
        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", inClass), Is.Empty);
    }

    [Test]
    public void ACheckInASetUp_NeedsTheWholeClassMarked()
    {
        string text = Class("    [SetUp]\n    public void SetUp()\n    {\n        Assert.Ignore(\"git isn't installed\");\n    }\n\n    [Test, Category(\"Integration\")]\n    public void Reads() { }");

        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", text).Select(u => u.Where), Is.EqualTo(new[] { "a SetUp: mark the class" }));
    }

    [Test]
    public void ACheckInAHelper_NeedsEveryTestThatCallsItMarked()
    {
        const string helper = "    private static int Live()\n    {\n        Assume.That(url, Is.Not.Null, \"BROWSER_URL isn't set\");\n        return 1;\n    }\n\n";
        string unmarked = Class(helper + "    [Test, Category(\"Live\")]\n    public void Reads()\n    {\n        var x = Live();\n    }\n\n    [Test]\n    public void Offline()\n    {\n    }");
        string marked = Class(helper + "    [Test, Category(\"Integration\")]\n    public void Reads()\n    {\n        var x = Live();\n    }");

        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", unmarked), Has.Count.EqualTo(1), "the test that calls the helper, not the one that doesn't");
        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", marked), Is.Empty);
    }

    [Test]
    public void AnAssumeThatIsNotAboutTheOutside_IsLeftAlone()
    {
        string text = Class("    [Test]\n    public void Reads()\n    {\n        Assume.That(assemblies, Is.Not.Empty, \"build the solution first\");\n    }");

        Assert.That(IntegrationTestsAreMarkedTests.Scan("a.cs", text), Is.Empty);
    }

    // The second signal (Louis's pick, "both signals"): a class that uses a real process, Mongo or the stand-in build leaves the test process, whether or not it checks first.
    private static string TestClass(string body, string attributes = "") =>
        $"{attributes}\npublic class Sample\n{{\n    [Test]\n    public void Runs()\n    {{\n        {body}\n    }}\n}}";

    [TestCase("var info = new ProcessStartInfo(\"git\");")]
    [TestCase("var process = Process.Start(\"git\");")]
    [TestCase("var client = new MongoClient(url);")]
    [TestCase("var dll = StandInBuild.Dll;")]
    [TestCase("var dll = RunnerBuild.Dll;")]
    public void AClassThatUsesTheOutside_WithNoMark_IsReported(string body)
    {
        var unmarked = IntegrationTestsAreMarkedTests.ScanUses("a.cs", TestClass(body), []);

        Assert.That(unmarked, Has.Count.EqualTo(1));
    }

    [Test]
    public void TheSameClassMarkedIntegration_IsNotReported() =>
        Assert.That(IntegrationTestsAreMarkedTests.ScanUses("a.cs", TestClass("var dll = StandInBuild.Dll;", "[Category(\"Integration\")]"), []), Is.Empty);

    [Test]
    public void AMentionInAStringOrAComment_IsNotAUse() =>
        Assert.That(IntegrationTestsAreMarkedTests.ScanUses("a.cs", TestClass("var text = \"ProcessStartInfo(\"; // Process.Start( and StandInBuild"), []), Is.Empty);

    [Test]
    public void AClassThatCallsAHelperThatStartsAProcess_IsReported() =>
        Assert.That(IntegrationTestsAreMarkedTests.ScanUses("a.cs", TestClass("var run = GitRunner.Run();"), ["GitRunner"]), Has.Count.EqualTo(1));

    [Test]
    public void AClassWithNoTest_IsNotReported() =>
        Assert.That(IntegrationTestsAreMarkedTests.ScanUses("a.cs", "public static class Build\n{\n    public static string Dll => StandInBuild.Dll;\n}", []), Is.Empty);
}
