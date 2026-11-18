using System.Text.Json;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content.Cost;
using LouisAgent.Web.Framework.Pages.Decide;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages.Decide;

/// <summary>
/// F161.1, step 12: Cost and evals, at <c>/cost-and-evals</c>, tells how a turn's cost is worked out, from the ledger
/// and the price table to the rand, with one request worked through, its numbers read from the real files; and the
/// evals' places, each saying only that they are being reworked and are coming (Louis: "It needs to be reworked").
/// </summary>
public class CostAndEvalsPageTests
{
    /// <summary>The coding demo's price table and exchange rates, which the worked example is read from.</summary>
    private const string Prices = "demos/louis-agent/config/prices.json", Rates = "demos/louis-agent/config/rates.json";

    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        return ctx;
    }

    /// <summary>A settings file of the repo, its comments allowed, as the engine reads it.</summary>
    private static JsonElement Read(string path) => JsonDocument.Parse(
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, path)),
        new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }).RootElement;

    /// <summary>It answers with its heading, its cost's sections, then the evals' places, and is in the menu.</summary>
    [Test]
    public void ThePage_HasItsHeading_AndItsSections()
    {
        using var ctx = Context();

        var cut = ctx.Render<CostAndEvalsPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("What a turn costs, to the cent."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id), Is.EqualTo(new[]
            {
                "top", "the-ledger", "the-price-table", "the-rand", "the-evals", "the-eval-studio"
            }));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/cost-and-evals"));
        });
    }

    /// <summary>
    /// The worked request's prices and rate are the price table's and the rates file's, with their dates, and the
    /// page shows the dollars and the rand they give.
    /// </summary>
    [Test]
    public void TheWorkedRequest_IsReadFromThePriceTableAndTheRates()
    {
        using var ctx = Context();
        var example = CostAndEvals.Example;
        var prices = Read(Prices);
        var model = prices.GetProperty("models").GetProperty(example.Model);
        var rates = Read(Rates);

        string page = ctx.Render<CostAndEvalsPage>().Find("section#the-rand").TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(example.InputPerMillion, Is.EqualTo(model.GetProperty("input").GetDecimal()));
            Assert.That(example.OutputPerMillion, Is.EqualTo(model.GetProperty("output").GetDecimal()));
            Assert.That(example.PricesAsOf, Is.EqualTo(prices.GetProperty("as_of").GetString()));
            Assert.That(example.RandPerDollar, Is.EqualTo(rates.GetProperty("rates").GetProperty("ZAR").GetDecimal()));
            Assert.That(example.RatesAsOf, Is.EqualTo(rates.GetProperty("as_of").GetString()));
            Assert.That(page, Does.Contain(example.DollarsText).And.Contain(example.RandText));
        });
    }

    /// <summary>
    /// The page opens on a request's path to rand (Louis's pick A of three), its numbers the worked request's own, so
    /// the picture and the steps under it can't disagree.
    /// </summary>
    [Test]
    public void ThePage_OpensOnARequestsPathToRand()
    {
        using var ctx = Context();
        var example = CostAndEvals.Example;

        string picture = ctx.Render<CostAndEvalsPage>().Find("section#top figure.diagram-figure svg").InnerHtml;

        Assert.That(picture, Does.Contain(example.DollarsText).And.Contain(example.RandText).And.Contain("prices.json")
            .And.Contain(example.PricesAsOf));
    }

    /// <summary>
    /// Each of the evals' places says they are being reworked and are coming, and claims nothing of how they work
    /// today: no list of terms and no doc behind it.
    /// </summary>
    [TestCase("the-evals")]
    [TestCase("the-eval-studio")]
    public void AnEvalsPlace_SaysOnlyComingSoon(string place)
    {
        using var ctx = Context();

        var section = ctx.Render<CostAndEvalsPage>().Find($"section#{place}");

        Assert.Multiple(() =>
        {
            Assert.That(section.TextContent, Does.Contain("coming soon"));
            Assert.That(section.QuerySelectorAll("ul.terms, a"), Is.Empty);
        });
    }
}
