using LouisAgent.Puls.Layout;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Tests.Layout;

/// <summary>
/// Puls's account layout puts the framework's card on Puls's own ground: the water behind it, as on the landing page
/// and the empty chat (FX64), with the logo above the page.
/// </summary>
public class AccountLayoutTests
{
    /// <summary>The water lies behind the card, and the page and the logo are in the card.</summary>
    [Test]
    public void TheCard_SitsOnTheWater_WithTheLogoAbove()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<AccountLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<h1>Teken in</h1>"))));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".puls-account > .wave-bg"), Has.Count.EqualTo(1), "the water, once");
            Assert.That(cut.FindAll(".puls-account .account-card h1"), Has.Count.EqualTo(1), "the page in the card");
            Assert.That(cut.FindAll(".account-brand .puls-logo"), Has.Count.EqualTo(1), "the logo above it");
        });
    }
}
