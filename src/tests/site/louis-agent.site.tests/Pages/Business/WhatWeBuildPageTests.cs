using LouisAgent.Site.Pages.Business;

namespace LouisAgent.Site.Tests.Pages.Business;

/// <summary>
/// What we build, in a buyer's words (F128.1, step 3): a website with an agent in its corner, and a custom agent for
/// a bigger job, then how it starts, with the way on to the plans and to Louis.
/// </summary>
public class WhatWeBuildPageTests
{
    /// <summary>The page opens with what we build, and says each thing in its own section.</summary>
    [Test]
    public void ThePage_SaysWhatWeBuild_OneThingASection()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<WhatWeBuildPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".band").Select(band => band.Id),
                Is.EqualTo(new[] { "top", "custom", "start" }));
            Assert.That(cut.Find("#top").TextContent,
                Does.Contain("website").And.Contain("answers your customers"));
            Assert.That(cut.Find("#custom").TextContent,
                Does.Contain("custom agent").And.Contain("your own environment"));
            Assert.That(cut.Find("#start").TextContent, Does.Contain("short call").And.Contain("made-up data"));
        });
    }

    /// <summary>A practice's agent passes people on: it never gives advice.</summary>
    [Test]
    public void APracticesAgent_NeverAdvises()
    {
        using var ctx = new BunitContext();

        Assert.That(ctx.Render<WhatWeBuildPage>().Find("#top").TextContent, Does.Contain("never gives advice"));
    }

    /// <summary>It leads on to the plans and to Louis.</summary>
    [TestCase("/pricing")]
    [TestCase("/contact")]
    public void ThePage_LeadsOn(string address)
    {
        using var ctx = new BunitContext();

        Assert.That(ctx.Render<WhatWeBuildPage>().FindAll($"a[href='{address}']"), Is.Not.Empty);
    }
}
