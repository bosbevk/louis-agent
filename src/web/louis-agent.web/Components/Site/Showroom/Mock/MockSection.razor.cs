using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// A section of a mocked business's website (F127.1): its heading, its words and its cards, at its own address on the
/// business's site (<c>/rooms</c>), so the business's agent can show it.
/// </summary>
/// <remarks>
/// The site writes the words and the cards; the section draws the heading, the row the cards stand in and the address,
/// so every mock's places carry their addresses the same way and the mock can find the one its agent shows.
/// </remarks>
public partial class MockSection
{
    /// <summary>Its address on the business's site: <c>/rooms</c>.</summary>
    [Parameter, EditorRequired]
    public string Path { get; set; } = "";

    /// <summary>Its heading: "Our rooms".</summary>
    [Parameter, EditorRequired]
    public string Heading { get; set; } = "";

    /// <summary>Its words, under the heading; none for a section of cards alone.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Its cards, in a row under its words; none for a section of words alone.</summary>
    [Parameter]
    public RenderFragment? Cards { get; set; }

    /// <summary>The place the mock's agent showed, handed down by the mock.</summary>
    [CascadingParameter(Name = SiteMock.ShownPlace)]
    public string? Shown { get; set; }

    /// <summary>A section, lit when it is the place its agent showed.</summary>
    private string Classes => Shown == Path ? "mock-section is-shown" : "mock-section";
}
