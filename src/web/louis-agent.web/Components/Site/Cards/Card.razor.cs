using LouisAgent.Web.Components.Site.Bands;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Cards;

/// <summary>
/// One card of a <see cref="CardDeck"/> (F113): what the row shows (a title and a line) and what opens under it (the
/// content). It registers with its deck and draws nothing itself. Since F118 it is an area the guide can open, named
/// by its title unless the site gives it an id.
/// </summary>
public partial class Card
{
    /// <summary>The deck this card belongs to, from the cascade.</summary>
    [CascadingParameter] public CardDeck Deck { get; set; } = default!;

    /// <summary>The words on the card.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The card's id on the page, for a link or the guide; none, and it is made from the title.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The id the card's button carries.</summary>
    public string AreaId => Id ?? AreaIds.FromWords(Title);

    /// <summary>One line under the title; none, and the card is its title.</summary>
    [Parameter] public string? Summary { get; set; }

    /// <summary>A small mono tag on the card, such as the project's name; none, and the card has no tag.</summary>
    [Parameter] public string? Tag { get; set; }

    /// <summary>The story the deck shows when the card is open.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Tells the deck this card exists, once.</summary>
    protected override void OnInitialized() => Deck.Add(this);
}
