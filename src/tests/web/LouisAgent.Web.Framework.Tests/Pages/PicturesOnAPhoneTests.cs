using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Web.Components.Graphs;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Graphs;
using LouisAgent.Web.Framework.Pages;
using LouisAgent.Web.Framework.Pages.Build;
using LouisAgent.Web.Framework.Pages.Decide;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F169.5 (Louis's pick "A: both drawn for a phone"): every picture the framework's nine pages draw has a narrow form,
/// so it reads on a phone without zooming, but the one sequence on How it works, which slides at its own size until
/// it is drawn for a phone of its own (F169.7).
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

    /// <summary>Every picture a page draws has a narrow form, but the one sequence, named.</summary>
    [TestCaseSource(nameof(NinePages))]
    public void EveryPicture_HasANarrowForm_ButTheSequence(Type page)
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
            foreach (var picture in pictures.Where(picture => picture != OneTurn.Source))
                Assert.That(NarrowFit.Has(picture), Is.True, picture.Title);
            Assert.That(NarrowFit.Has(OneTurn.Source), Is.False, "the sequence slides until F169.7");
        });
    }
}
