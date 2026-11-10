using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// One showroom business's demo (F128.2; since F127.1 on the framework's stage): its recorded conversation played on a
/// mock of its own website, in its sample brand, with the cues of what to watch beside it and the offer under it. Each
/// page names its business, its brand, its turns and its cues; the shape is this one.
/// </summary>
/// <remarks>
/// The page's one piece of state is the turn the play has reached: the recorded chat says each turn as it starts, and
/// the cues light up to it, so the two never know each other.
/// </remarks>
public partial class BusinessDemo
{
    /// <summary>The demo's colour, the one it wears in the showroom's rail.</summary>
    [Parameter, EditorRequired]
    public TileAccent Accent { get; set; }

    /// <summary>The kind of business, over the heading: "A restaurant".</summary>
    [Parameter, EditorRequired]
    public string Kicker { get; set; } = "";

    /// <summary>The business's name, said to be made up.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>What the page shows, in a few words: the page's main heading.</summary>
    [Parameter, EditorRequired]
    public string Heading { get; set; } = "";

    /// <summary>The scene: who asks what.</summary>
    [Parameter, EditorRequired]
    public string Lede { get; set; } = "";

    /// <summary>The business's own address, made up: <c>wheatfieldkitchen.example</c>.</summary>
    [Parameter, EditorRequired]
    public string Address { get; set; } = "";

    /// <summary>Its sample brand's colours.</summary>
    [Parameter, EditorRequired]
    public MockColours Colours { get; set; } = null!;

    /// <summary>Its sample brand's typeface.</summary>
    [Parameter]
    public MockFont Font { get; set; }

    /// <summary>Its sample brand's mark, drawn for it.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Mark { get; set; } = null!;

    /// <summary>The few words under its name.</summary>
    [Parameter, EditorRequired]
    public string Tagline { get; set; } = "";

    /// <summary>The words of its website's menu.</summary>
    [Parameter]
    public IReadOnlyList<string> NavItems { get; set; } = [];

    /// <summary>Its website's hero line, from what its agent knows.</summary>
    [Parameter, EditorRequired]
    public string HeroLine { get; set; } = "";

    /// <summary>A few words under the hero line, from what its agent knows.</summary>
    [Parameter]
    public string? HeroNote { get; set; }

    /// <summary>Its website's look (Louis: "give each one a different look").</summary>
    [Parameter]
    public MockLook Look { get; set; }

    /// <summary>Its website's scene, drawn for it.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Scene { get; set; } = null!;

    /// <summary>Its website's call to action: "Book a table".</summary>
    [Parameter, EditorRequired]
    public string HeroAction { get; set; } = "";

    /// <summary>A few facts under its website's hero, from what its agent knows.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Facts { get; set; } = [];

    /// <summary>Its website's places, each at its address, the places its agent can show.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Places { get; set; } = null!;

    /// <summary>The conversation, a turn at a time.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RecordedTurn> Turns { get; set; } = [];

    /// <summary>What to watch for, each about a turn.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DemoCue> Cues { get; set; } = [];

    /// <summary>The turn the play has reached; none before it starts.</summary>
    private int _reached;

    /// <summary>What the demo is, over its title: the kind and the business.</summary>
    private string KickerLine => $"{Kicker} · {Business}";

    /// <summary>The widget's title on the business's website.</summary>
    private string AgentLabel => $"{Business} · ask us";

    /// <summary>Notes the turn the play has reached, so the cues light up to it.</summary>
    private void Reach(int turn) => _reached = turn;
}
