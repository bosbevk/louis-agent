using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Cards;

/// <summary>
/// A card's looks (Louis's architecture figure, CL60.1): a highlighted card centred in the accent's tint, as the core
/// engine stands out between the layers, and a heading centred with no box, as the application on top.
/// </summary>
public class CardLooksTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Louis's architecture figure: the application on top, its identity and packs, the core engine across the
    /// width, and the model providers and the evaluation under it.
    /// </summary>
    private static DiagramSource Figure() => new(DiagramKind.Cards, "The architecture in layers",
        "Conceptual summary of the published architecture, not a complete dependency diagram.", Cards: new CardsSpec(
            [
                new CardRow([
                    new Card("app", DiagramIcon.Code, "Your application", null,
                        "The business problem and application-specific configuration", CardLook.Heading)
                ]),
                new CardRow([
                    new Card("identity", DiagramIcon.User, "Identity", null,
                        "Who the agent is, its roles, voice and rules."),
                    new Card("packs", DiagramIcon.Box, "Packs", null,
                        "What the agent can do through tools and integrations.")
                ]),
                new CardRow([
                    new Card("engine", DiagramIcon.Chip, "Core engine", null,
                        "Tool loop · streaming · guards · provider integration · delegation · usage accounting",
                        CardLook.Highlight)
                ]),
                new CardRow([
                    new Card("providers", DiagramIcon.Cloud, "Model providers", null,
                        "Claude, Ollama and OpenAI-compatible endpoints."),
                    new Card("evaluation", DiagramIcon.Checks, "Evaluation and measurement", null,
                        "Tests, recorded runs, request traces and cost accounting.")
                ])
            ],
            [
                new CardLink("app", "identity"),
                new CardLink("app", "packs"),
                new CardLink("identity", "engine"),
                new CardLink("packs", "engine"),
                new CardLink("engine", "providers"),
                new CardLink("engine", "evaluation")
            ]));

    /// <summary>Louis's architecture figure, drawn, matches its golden file.</summary>
    [Test]
    public void TheArchitectureFigure_MatchesTheGoldenFile() =>
        Golden.Match("looks.svg", DiagramRenderers.Render(Figure()));

    /// <summary>
    /// The highlighted card wears the accent's tint and line in both looks, and its title and words are centred on it.
    /// </summary>
    [Test]
    public void AHighlightedCard_IsCentred_InTheAccentsTint()
    {
        var svg = XDocument.Parse(DiagramRenderers.Render(Figure()));
        string style = svg.Descendants(Svg + "style").Single().Value;

        var box = svg.Descendants(Svg + "rect").Single(rect => (string)rect.Attribute("class")! == "card highlight");
        double centre = Number(box, "x") + Number(box, "width") / 2;
        var title = Word(svg, "Core engine");
        Assert.Multiple(() =>
        {
            Assert.That(style, Does.Contain(".highlight {"));
            Assert.That(style, Does.Contain(SvgTheme.Site.Light.Accent).And.Contain(SvgTheme.Site.Dark.Accent));
            Assert.That(style, Does.Contain(SvgTheme.Site.Light.Tint).And.Contain(SvgTheme.Site.Dark.Tint));
            Assert.That((string?)title.Attribute("text-anchor"), Is.EqualTo("middle"));
            Assert.That(Number(title, "x"), Is.EqualTo(centre));
            Assert.That((string?)Word(svg, "Tool loop", startsWith: true).Attribute("text-anchor"),
                Is.EqualTo("middle"));
        });
    }

    /// <summary>A heading has no box of its own, and its title is centred in the frame.</summary>
    [Test]
    public void AHeading_HasNoBox_AndIsCentred()
    {
        var svg = XDocument.Parse(DiagramRenderers.Render(Figure()));

        var cards = svg.Descendants(Svg + "rect")
            .Where(rect => ((string)rect.Attribute("class")!).StartsWith("card", StringComparison.Ordinal));
        var frame = svg.Descendants(Svg + "rect").Single(rect => (string)rect.Attribute("class")! == "frame");
        var title = Word(svg, "Your application");
        Assert.Multiple(() =>
        {
            Assert.That(cards.Count(), Is.EqualTo(5), "six cards, the heading without a box");
            Assert.That((string?)title.Attribute("text-anchor"), Is.EqualTo("middle"));
            Assert.That(Number(title, "x"), Is.EqualTo(Number(frame, "width") / 2));
        });
    }

    /// <summary>A plain card keeps its words from the left, beside the looks that centre theirs.</summary>
    [Test]
    public void APlainCard_StillStartsFromTheLeft()
    {
        var svg = XDocument.Parse(DiagramRenderers.Render(Figure()));

        Assert.That((string?)Word(svg, "Identity").Attribute("text-anchor"), Is.EqualTo("start"));
    }

    /// <summary>The one text element that reads these words, or starts with them.</summary>
    private static XElement Word(XDocument svg, string words, bool startsWith = false) =>
        svg.Descendants(Svg + "text")
            .Single(text => startsWith ? text.Value.StartsWith(words, StringComparison.Ordinal) : text.Value == words);

    /// <summary>A number an element's attribute holds.</summary>
    private static double Number(XElement element, string name) =>
        double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
}
