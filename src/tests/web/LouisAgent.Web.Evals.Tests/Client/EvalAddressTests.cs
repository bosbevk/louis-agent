using LouisAgent.Web.Evals.Client;

namespace LouisAgent.Web.Evals.Tests.Client;

/// <summary>
/// Every address of the eval pages and every eval route is built in one place, its names escaped, so no page joins
/// one by hand.
/// </summary>
public class EvalAddressTests
{
    /// <summary>The evals' page, a run's, and a conversation's with a repeat and without one.</summary>
    [Test]
    public void ThePages_HaveTheirAddresses()
    {
        Assert.That(EvalAddress.EvalsPage, Is.EqualTo("/admin/evals"));
        Assert.That(EvalAddress.RunPageOf("guide", "20261123-063633-guide"),
            Is.EqualTo("/admin/evals/guide/runs/20261123-063633-guide"));
        Assert.That(EvalAddress.ConversationPageOf("guide", "20261123-063633-guide", "guide-what-is", 2),
            Is.EqualTo("/admin/evals/guide/runs/20261123-063633-guide/guide-what-is/2"));
        Assert.That(EvalAddress.ConversationPageOf("guide", "20261123-063633-guide", "guide-what-is"),
            Is.EqualTo("/admin/evals/guide/runs/20261123-063633-guide/guide-what-is"));
    }

    /// <summary>The three routes the pages read, beside the pages' addresses.</summary>
    [Test]
    public void TheRoutes_HaveTheirAddresses()
    {
        Assert.That(EvalAddress.EvalsRoute, Is.EqualTo("v1/admin/evals"));
        Assert.That(EvalAddress.RunRouteOf("20261123-063633-guide"),
            Is.EqualTo("v1/admin/evals/runs/20261123-063633-guide"));
        Assert.That(EvalAddress.RunsRouteOf("guide"), Is.EqualTo("v1/admin/evals/guide/runs"));
    }

    /// <summary>A name a URL can't hold as it is comes out escaped, in a page's address and in a route.</summary>
    [Test]
    public void AName_IsEscaped_InAPageAndInARoute()
    {
        Assert.That(EvalAddress.ConversationPageOf("a b", "r/1", "what?"),
            Is.EqualTo("/admin/evals/a%20b/runs/r%2F1/what%3F"));
        Assert.That(EvalAddress.RunsRouteOf("a b"), Is.EqualTo("v1/admin/evals/a%20b/runs"));
        Assert.That(EvalAddress.RunRouteOf("r/1"), Is.EqualTo("v1/admin/evals/runs/r%2F1"));
    }
}
