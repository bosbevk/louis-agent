namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// A site's router lists what the site opted into (CL57): a library a site adds services for in its program is in
/// its router's assemblies too, or its pages answer "not found", which is what happened to the account pages until
/// this rule. Every site on the framework is read from disk, so a new one comes under the rule by itself.
/// </summary>
public class SiteRouterTests
{
    private static IEnumerable<TestCaseData> Sites() =>
        FrameworkWebSites.Folders().Select(folder => new TestCaseData(folder).SetArgDisplayNames(FrameworkWebSites.NameOf(folder)));

    [TestCaseSource(nameof(Sites))]
    public void ALibraryTheSiteAddsServicesFor_IsInItsRouter(string folder)
    {
        string program = File.ReadAllText(Path.Combine(folder, "Program.cs"));
        string router = File.ReadAllText(Path.Combine(folder, "App.razor"));

        Assert.Multiple(() =>
        {
            if (program.Contains("AddLouisAgentAccounts("))
                Assert.That(router, Does.Contain("AccountLibrary.Assembly"), "the account pages need the router");
            if (program.Contains("AddLouisAgentEvals("))
                Assert.That(router, Does.Contain("EvalLibrary.Assembly"), "the studio's pages need the router");
        });
    }
}
