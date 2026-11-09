using LouisAgent.Web.Components.Recorded;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// One showroom business's demo page body (F128.2): its words beside its recorded conversation, played as its customer
/// would see it, and the offer at the end. Each page names its business and its turns; the shape is this one.
/// </summary>
public partial class BusinessDemo
{
    /// <summary>The kind of business, over the heading.</summary>
    [Parameter, EditorRequired]
    public string Kicker { get; set; } = "";

    /// <summary>The business's name, said to be made up.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>What the page shows, in a few words.</summary>
    [Parameter, EditorRequired]
    public string Heading { get; set; } = "";

    /// <summary>The scene: who asks what.</summary>
    [Parameter, EditorRequired]
    public string Lede { get; set; } = "";

    /// <summary>What to watch for, as list items.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Points { get; set; } = default!;

    /// <summary>The window's title line: the business's site.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>The conversation, a turn at a time.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RecordedTurn> Turns { get; set; } = [];
}
