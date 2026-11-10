using LouisAgent.Choices;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// A mock of a business's own website (F127.1, Louis's pick 3a: a header, one hero line and the agent): a browser bar
/// with its address, its name and menu in its brand colour, a line of its hero, and its agent chatting in a widget in
/// the corner. A visitor sees the agent where its customers would meet it, on a website like their own.
/// </summary>
/// <remarks>
/// Device mock-up, as product pages show an app in a phone or a browser (the research: Copilot Studio's demo website,
/// Gardenia's "how their environment would look"). The business's sample brand is the page's data (its colours as
/// <see cref="MockColours"/>, set on the mock as variables its styles read, its mark, tagline and
/// <see cref="MockFont"/>), so its look is its own and never the site's theme.
/// </remarks>
public partial class SiteMock
{
    /// <summary>The business's address in the browser bar: <c>the-bakery.example</c>.</summary>
    [Parameter, EditorRequired]
    public string Address { get; set; } = "";

    /// <summary>The business's name, at the start of its header.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>The business's own colours.</summary>
    [Parameter, EditorRequired]
    public MockColours Colours { get; set; } = null!;

    /// <summary>Its logo mark, drawn for it in SVG (its sample brand); none for a business with no mark.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The few words under its name; none for a business with no tagline.</summary>
    [Parameter]
    public string? Tagline { get; set; }

    /// <summary>The typeface its website wears; the serif unless it has its own.</summary>
    [Parameter]
    public MockFont Font { get; set; }

    /// <summary>The words of its menu.</summary>
    [Parameter]
    public IReadOnlyList<string> NavItems { get; set; } = [];

    /// <summary>Its hero's line: "Fresh bread before seven."</summary>
    [Parameter, EditorRequired]
    public string HeroLine { get; set; } = "";

    /// <summary>A few words under the hero's line; none for a hero of one line.</summary>
    [Parameter]
    public string? HeroNote { get; set; }

    /// <summary>The widget's title: "The Bakery · ask us".</summary>
    [Parameter]
    public string AgentLabel { get; set; } = "";

    /// <summary>The agent: its recorded chat.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Agent { get; set; } = null!;

    /// <summary>The mock's classes: a mock, in its business's typeface.</summary>
    private string Classes => $"site-mock font-{Font.Wire()}";

    /// <summary>The business's colours as the variables the mock's styles read.</summary>
    private string ColourStyle =>
        $"--mock-brand: {Colours.Brand}; --mock-on-brand: {Colours.OnBrand}; --mock-page: {Colours.Page}; " +
        $"--mock-ink: {Colours.Ink}";
}
