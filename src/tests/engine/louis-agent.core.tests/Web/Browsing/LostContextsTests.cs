using LouisAgent.Core.Web.Browsing;
using PuppeteerSharp;

namespace LouisAgent.Core.Tests.Web.Browsing;

/// <summary>
/// PuppeteerSharp fails a task nothing waits on every time a page or an iframe goes before its script context exists.
/// That one is the library's own and costs nothing; anything else from the library is still worth an error.
/// </summary>
public class LostContextsTests
{
    /// <summary>The library's failure, word for word, is the one that's expected.</summary>
    [Test]
    public void TheLibrarysOwnFailure_IsExpected() =>
        Assert.That(LostContexts.Are(new PuppeteerException("Execution Context was destroyed")), Is.True);

    /// <summary>Another failure of the library's, or the same words from elsewhere, is not.</summary>
    [Test]
    public void AnyOtherFailure_IsNot()
    {
        Assert.That(LostContexts.Are(new PuppeteerException("Navigation timeout of 30000 ms exceeded")), Is.False);
        Assert.That(LostContexts.Are(new InvalidOperationException("Execution Context was destroyed")), Is.False);
    }
}
