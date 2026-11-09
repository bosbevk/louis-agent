using LouisAgent.Web.Components.Site.Meta;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Tests.Components.Site.Meta;

/// <summary>
/// A page says what it is about (F121): its description goes into the page's head, where the browser shows it and
/// the framework's prerenderer reads it for a search engine.
/// </summary>
public class PageDescriptionTests
{
    /// <summary>The description lands in the head as a meta tag, its words escaped.</summary>
    [Test]
    public void TheDescription_LandsInTheHead()
    {
        using var ctx = new BunitContext();
        // The head's outlet asks the page's script for the old title first; here nothing answers.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent<HeadOutlet>(0);
            builder.CloseComponent();
            builder.OpenComponent<PageDescription>(1);
            builder.AddComponentParameter(2, nameof(PageDescription.Text), "Agents in C# & \"measured\".");
            builder.CloseComponent();
        });

        cut.WaitForAssertion(() => Assert.That(cut.Find("meta[name=description]").GetAttribute("content"),
            Is.EqualTo("Agents in C# & \"measured\".")));
    }
}
