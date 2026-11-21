using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.ShopPrices.Tools;

namespace LouisAgent.Packs.ShopPrices.Tests;

/// <summary>The pack is one toolset with its guide, a feed and an upload, and needs nothing set to load.</summary>
public class ShopPricesPackTests
{
    /// <summary>Its name, its one toolset and its guide are the pack's own, read through the one reader.</summary>
    [Test]
    public void TheToolset_AndItsGuide_AreThePacks()
    {
        var pack = new ShopPricesPack(new ShopPricesOptions());

        Assert.That(pack.Name, Is.EqualTo(ShopPricesToolset.ShopPrices.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { ShopPricesToolset.ShopPrices.Wire() }));
        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/shop-prices.md"));
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/shop-prices.md"),
            Does.Contain("Route here a request for roughly what something costs"));
    }

    /// <summary>No setting and no browser: it loads wherever it is named, and its tool is safe beside any other.</summary>
    [Test]
    public void ItNeedsNoSetting_AndRunsInParallel()
    {
        var toolset = new ShopPricesPack(new ShopPricesOptions()).Toolsets[0];

        Assert.That(toolset.MissingSettings(null!), Is.Empty);
        Assert.That(toolset.ParallelSafe, Is.True);
        Assert.That(toolset.ToolTypes, Is.EqualTo(new[] { typeof(ShopPriceTools) }));
    }

    /// <summary>It publishes one feed and takes one upload, both named for the pack (ADR 0070, F55).</summary>
    [Test]
    public void ItPublishesOneFeed_AndTakesOneUpload()
    {
        var pack = new ShopPricesPack(new ShopPricesOptions());

        Assert.That(pack.Feeds.Select(f => f.Name), Is.EqualTo(new[] { ShopPricesToolset.ShopPrices.Wire() }));
        Assert.That(pack.Imports.Select(i => i.Name), Is.EqualTo(new[] { ShopPricesToolset.ShopPrices.Wire() }));
    }
}
