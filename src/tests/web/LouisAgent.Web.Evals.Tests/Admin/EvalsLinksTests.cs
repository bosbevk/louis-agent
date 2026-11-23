using LouisAgent.Web.Evals.Admin;
using LouisAgent.Web.Evals.Client;

namespace LouisAgent.Web.Evals.Tests.Admin;

/// <summary>The evals' link in the admin's sidebar.</summary>
public class EvalsLinksTests
{
    /// <summary>The link goes to the evals' page.</summary>
    [Test]
    public void TheLink_GoesToTheEvalsPage()
    {
        using var ctx = new BunitContext();

        var links = ctx.Render<EvalsLinks>();

        Assert.That(links.Find("a.side-link").GetAttribute("href"), Is.EqualTo(EvalAddress.EvalsPage));
        Assert.That(links.Find("a.side-link").TextContent, Does.Contain("Evals"));
    }
}
