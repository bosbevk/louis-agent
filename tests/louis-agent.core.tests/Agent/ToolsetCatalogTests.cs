using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests.Agent;

// F33 step 4 (Louis: optional toolsets): Puls hands the cook `web` and `shops?`. A name ending in `?` loads when this
// instance has the toolset and is skipped when it doesn't, so cooking works on a deployment without SHOP_CATALOGUES
// (the sold endpoint: development and benchmarks first, ADR 0023). An unknown name stays an error, `?` or not: a typo
// is never skipped in silence.
public class ToolsetCatalogTests
{
    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    private static readonly ToolsetContext WithoutShops = new(new AgentOptions(), new NoSearch());
    private static readonly ToolsetContext WithShops = new(new AgentOptions { ShopCatalogues = "checkers", BrowserUrl = "http://puls-browser:9222" }, new NoSearch());

    private static string[] Resolved(IEnumerable<string> names, ToolsetContext context) =>
        ToolsetCatalog.Resolve(names, context).Select(t => t.Name).ToArray();

    [Test]
    public void AnOptionalToolsetThisInstanceHas_Loads() =>
        Assert.That(Resolved(["web", "shops?"], WithShops), Is.EqualTo(new[] { "web", "shops" }));

    [Test]
    public void AnOptionalToolsetThisInstanceLacks_IsSkipped() =>
        Assert.That(Resolved(["web", "shops?"], WithoutShops), Is.EqualTo(new[] { "web" }));

    [Test]
    public void ARequiredToolsetThisInstanceLacks_IsStillRefused() =>
        Assert.That(() => Resolved(["web", "shops"], WithoutShops),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("SHOP_CATALOGUES"));

    [Test]
    public void AnUnknownName_IsRefused_EvenOptional() =>
        Assert.That(() => Resolved(["web", "shop?"], WithShops),
            Throws.TypeOf<ToolsetException>().With.Message.Contain("shop").And.Message.Contain("Valid names"));

    [TestCase(new[] { "shops?", " Shops? " }, new[] { "shops?" })]
    [TestCase(new[] { "shops?", "shops" }, new[] { "shops" })]
    [TestCase(new[] { "web", "shops?" }, new[] { "shops?", "web" })]
    public void Normalise_KeepsTheMark_AndARequiredNameWins(string[] names, string[] expected) =>
        Assert.That(ToolsetCatalog.Normalise(names), Is.EqualTo(expected));

    [TestCase("shops?", "shops")]
    [TestCase("web", "web")]
    public void TheNameOfAnOptionalToolset_IsWithoutItsMark(string name, string expected) =>
        Assert.That(ToolsetCatalog.NameOf(name), Is.EqualTo(expected));
}
