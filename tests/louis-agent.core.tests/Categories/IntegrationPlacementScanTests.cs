namespace LouisAgent.Core.Tests.Categories;

// F18 step 7.6.3: the tests that leave their process live in their own projects, `{name}.integration-tests`, apart from the unit tests. The ratchet reads each test file's mark and its project.
public class IntegrationPlacementScanTests
{
    private const string MarkedClass = "[Category(\"Integration\")]\npublic class Sample\n{\n    [Test]\n    public void Runs() { }\n}";
    private const string UnmarkedClass = "public class Sample\n{\n    [Test]\n    public void Runs() { }\n}";

    [Test]
    public void AMarkedTest_InAUnitProject_IsMisplaced() =>
        Assert.That(IntegrationPlacementTests.Misplaced("tests/louis-agent.core.tests/Tools/Sample.cs", MarkedClass), Has.Count.EqualTo(1));

    [Test]
    public void AnUnmarkedTestClass_InAnIntegrationProject_IsMisplaced() =>
        Assert.That(IntegrationPlacementTests.Misplaced("tests/louis-agent.core.integration-tests/Tools/Sample.cs", UnmarkedClass), Has.Count.EqualTo(1));

    [Test]
    public void AMarkedClass_InAnIntegrationProject_AndAnUnmarkedOne_InAUnitProject_AreWhereTheyBelong()
    {
        Assert.That(IntegrationPlacementTests.Misplaced("tests/louis-agent.core.integration-tests/Tools/Sample.cs", MarkedClass), Is.Empty);
        Assert.That(IntegrationPlacementTests.Misplaced("tests/louis-agent.core.tests/Tools/Sample.cs", UnmarkedClass), Is.Empty);
    }

    [Test]
    public void AHelperWithNoTest_IsLeftWhereItIs() =>
        Assert.That(IntegrationPlacementTests.Misplaced("tests/louis-agent.core.integration-tests/Tools/Build.cs", "public static class Build\n{\n    public static int N => 1;\n}"), Is.Empty);

    [Test]
    public void ASharedLibrarysContract_IsNotAUnitOrIntegrationTest() =>
        Assert.That(IntegrationPlacementTests.Misplaced("tests/louis-agent.api.testing/Contracts/Store.cs", UnmarkedClass), Is.Empty);
}
