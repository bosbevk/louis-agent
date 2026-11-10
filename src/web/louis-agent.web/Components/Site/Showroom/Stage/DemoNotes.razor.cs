using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Stage;

/// <summary>
/// What a custom agent could add for a business like the demo's (F127.1, Louis: "we must make notes on the demo screen
/// for instance we could integrate with the booking system if they have one etc. Custom Agent"): a kicker, a heading
/// and a card a note, under the demo, so a visitor sees how far theirs could go.
/// </summary>
public partial class DemoNotes
{
    /// <summary>The few words over the heading: "Make it yours".</summary>
    [Parameter]
    public string Kicker { get; set; } = "";

    /// <summary>The heading: "A custom agent for your guesthouse could also".</summary>
    [Parameter, EditorRequired]
    public string Heading { get; set; } = "";

    /// <summary>The notes, in the order they show.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DemoNote> Notes { get; set; } = [];
}
