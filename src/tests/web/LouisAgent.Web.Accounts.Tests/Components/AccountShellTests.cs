using LouisAgent.Web.Accounts.Components;

namespace LouisAgent.Web.Accounts.Tests.Components;

/// <summary>The account pages' frame: one card, the site's brand above the page, and no gap when there is no brand.</summary>
public class AccountShellTests
{
    [Test]
    public void TheBrand_IsAboveThePage_InOneCard()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AccountShell>(parameters => parameters
            .Add(shell => shell.Brand, "<b class=\"logo\">Brand</b>")
            .Add(shell => shell.BrandTitle, "Home")
            .Add(shell => shell.ChildContent, "<h1>Sign in</h1>"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".account-shell .account-card"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".account-brand").GetAttribute("title"), Is.EqualTo("Home"));
            Assert.That(cut.Find(".account-card").Children.Select(c => c.TagName.ToLowerInvariant()),
                Is.EqualTo(new[] { "a", "h1" }), "the brand first, then the page");
        });
    }

    [Test]
    public void NoBrand_LeavesNoGap()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AccountShell>(parameters => parameters.Add(shell => shell.ChildContent, "<p>x</p>"));

        Assert.That(cut.FindAll(".account-brand"), Is.Empty);
    }
}
