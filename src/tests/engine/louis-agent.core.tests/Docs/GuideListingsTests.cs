namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// The guides hold the programs a developer asks the site's guide for (FX96, Louis: "people do not have access to the
/// source, the agent should have outputted a C# styled program.cs which you can copy paste"): the API's program is in
/// the API guide as a C# listing the docs tool can read back whole, so the guide never answers with a path to a file
/// nobody can open.
/// </summary>
public class GuideListingsTests
{
    private static string Guide => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/docs/guides/change-the-api.md"));

    /// <summary>The API guide has one complete program in a C# block: the host, the API, the key check, the web app, the routes, the run.</summary>
    [Test]
    public void TheApiGuide_HoldsTheWholeProgram_AsCSharp()
    {
        string guide = Guide;
        int start = guide.IndexOf("```csharp", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "a C# block");
        string listing = guide[start..guide.IndexOf("```", start + 9, StringComparison.Ordinal)];

        Assert.Multiple(() =>
        {
            foreach (string line in new[] { "AgentHost.Build(", "builder.AddLouisAgentApi(host)", "app.UseLouisAgentKey()", "app.MapStaticAssets()", "app.MapFallbackToFile(\"index.html\")", "app.MapLouisAgentApi(host)", "app.Run()" })
                Assert.That(listing, Does.Contain(line));
            Assert.That(listing, Does.Contain("using LouisAgent.Api;"), "the usings a paste needs");
        });
    }
}
