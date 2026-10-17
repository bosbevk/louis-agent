using LouisAgent.Core.Tools.Shops;
using LouisAgent.Core.Tools.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tests.Tools.Shops;

// F33 step 2: a catalogue per shop a deployment enables (ShopKind, an enum: ADR 0024), each on the guarded browser.
// Which shops are enabled comes from SHOP_CATALOGUES in step 3; none enabled is none created.
public class ShopCatalogueFactoryTests
{
    private sealed class NoBrowser : IWebBrowser
    {
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserVisit(url.ToString(), "", []));
    }

    [Test]
    public void AnEnabledShop_GetsItsCatalogue()
    {
        var catalogues = ShopCatalogueFactory.Create([ShopKind.Checkers], new NoBrowser());

        Assert.That(catalogues[ShopKind.Checkers], Is.TypeOf<CheckersCatalogue>());
        Assert.That(catalogues[ShopKind.Checkers].Shop, Is.EqualTo(ShopKind.Checkers));
    }

    [Test]
    public void NoShopEnabled_IsNoCatalogue() =>
        Assert.That(ShopCatalogueFactory.Create([], new NoBrowser()), Is.Empty);

    [Test]
    public void TheShops_AreTheEnumsNames() =>
        Assert.That(ShopCatalogueFactory.SupportedShops, Is.EqualTo(new[] { "checkers" }));
}
