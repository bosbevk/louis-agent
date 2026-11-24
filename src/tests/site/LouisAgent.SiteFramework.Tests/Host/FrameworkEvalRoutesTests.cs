namespace LouisAgent.SiteFramework.Tests.Host;

/// <summary>
/// The framework site's host takes the evals as louis-agent.dev's does (F126.5.2): their services beside the API's,
/// and their routes on the API's group behind the admin key, so the framework Orbi is measured on the host that
/// serves it. The routes themselves, the key check included, are <c>LouisAgent.Api.Evals</c>'s, tested there.
/// </summary>
public class FrameworkEvalRoutesTests
{
    /// <summary>The framework host's program, as the image builds it.</summary>
    private static string Program() =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/LouisAgent.SiteFrameworkApi/Program.cs"));

    /// <summary>The program adds the evals' services and maps their routes with the admin's key.</summary>
    [Test]
    public void TheProgram_TakesTheEvals_BehindTheAdminKey()
    {
        string program = Program();

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddLouisAgentEvals(host"));
            Assert.That(program, Does.Contain("EvalsOptions.FromEnvironment()"));
            Assert.That(program, Does.Contain("AdminOptions.FromEnvironment().AdminKey"));
            Assert.That(program, Does.Contain("MapLouisAgentEvals(adminKey)"));
        });
    }
}
