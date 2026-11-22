namespace LouisAgent.Api.Tests;

/// <summary>
/// The API's tests prove the framework's API, so they run on the framework alone (F166.1, Louis's pick A of three):
/// a test project references no product, nothing under <c>demos/</c> and none of the packs that leave with Puls, so
/// Puls can move to its own repository without taking the API's tests with it.
/// </summary>
/// <remarks>Plain string checks on the project file's lines, no regex (ADR 0057).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0076-core-ships-no-agent.md">ADR 0076: core ships no agent</seealso>
public class TestsNameNoProductTests
{
    /// <summary>
    /// What a test project of the framework's never references: a demo by its folder or by Puls's projects' names,
    /// since a reference from beside them climbs no higher than <c>..\..\identity</c>, or a pack that leaves with Puls.
    /// </summary>
    private static readonly string[] Products =
    [
        @"demos\", "demos/", "identity.puls", "puls-eval-runner", "host-packs", "pack.recipes", "pack.shop-prices",
        "pack.where-to-buy"
    ];

    /// <summary>Where the framework's API test projects sit, beside the framework's other hosts' tests.</summary>
    private const string TestsFolder = "src/tests/hosts";

    /// <summary>Each project reference in a test project's file, by the path it names.</summary>
    private static IEnumerable<string> References(string project) =>
        File.ReadAllLines(Path.Combine(ApiTestHost.RepoRoot, TestsFolder, project, $"{project}.csproj"))
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("<ProjectReference Include=\"", StringComparison.Ordinal));

    /// <summary>The project's references that name a product, which must be none.</summary>
    [TestCase("LouisAgent.Api.Testing")]
    [TestCase("LouisAgent.Api.Tests")]
    [TestCase("LouisAgent.Api.IntegrationTests")]
    [TestCase("LouisAgent.Api.TestRunner")]
    public void ATestProject_ReferencesNoProduct(string project)
    {
        var named = References(project)
            .Where(reference => Products.Any(product => reference.Contains(product, StringComparison.Ordinal)));

        Assert.That(named, Is.Empty);
    }
}
