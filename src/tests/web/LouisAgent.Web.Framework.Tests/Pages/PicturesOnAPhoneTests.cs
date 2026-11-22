using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Web.Components.Graphs;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Pages;
using LouisAgent.Web.Framework.Pages.Build;
using LouisAgent.Web.Framework.Pages.Decide;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F169.5 and F169.7 (Louis's pick "A: both drawn for a phone"): every picture the framework's nine pages draw has a
/// narrow form, so it reads on a phone without zooming and nothing slides, the sequence on How it works drawn as its
/// steps down the page.
/// </summary>
public class PicturesOnAPhoneTests
{
    /// <summary>The nine pages.</summary>
    private static readonly Type[] NinePages =
    [
        typeof(OverviewPage), typeof(GettingStartedPage), typeof(HowItWorksPage), typeof(FeaturesPage),
        typeof(BuildYourOwnPage), typeof(RunItPage), typeof(CostAndEvalsPage), typeof(ExamplesPage),
        typeof(ComparisonPage)
    ];

    /// <summary>Every picture a page draws has a narrow form, the sequence too.</summary>
    [TestCaseSource(nameof(NinePages))]
    public void EveryPicture_HasANarrowForm(Type page)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });
        var pictures = cut.FindComponents<DiagramFigure>().Select(figure => figure.Instance.Source).ToList();

        Assert.Multiple(() =>
        {
            foreach (var picture in pictures) Assert.That(NarrowFit.Has(picture), Is.True, picture.Title);
        });
    }
}
