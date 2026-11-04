using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Packs.Recipes.Tests.Providers;

/// <summary>
/// A browser for the tests that answers every address with one saved page, remembers what it was asked for, or fails
/// the way the real one does.
/// </summary>
/// <remarks>
/// The page's own address is what the visit reports, as the real browser does after any redirect.
/// </remarks>
internal sealed class FakeSiteBrowser(string html = "", Exception? fails = null) : IWebBrowser
{
    /// <summary>The addresses it was asked to open, in order.</summary>
    public List<Uri> Requested { get; } = [];

    /// <summary>Opens a saved page, or throws what it was told to.</summary>
    public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
        string? script = null, string? readySelector = null, CancellationToken cancellationToken = default)
    {
        Requested.Add(url);
        return fails is null
            ? Task.FromResult(new BrowserVisit(url.ToString(), html, []))
            : Task.FromException<BrowserVisit>(fails);
    }

    /// <summary>Reads one saved search page from the Fixtures folder beside the tests.</summary>
    public static string Fixture(string name) => File.ReadAllText(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Providers", "Fixtures", name + ".html"));
}
