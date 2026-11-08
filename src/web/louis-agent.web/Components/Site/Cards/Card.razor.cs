using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Cards;

/// <summary>
/// One card of a <see cref="CardDeck"/> (F113): what the row shows (a title and a line) and what opens under it (the
/// content). It registers with its deck and draws nothing itself.
/// </summary>
public partial class Card
{
    /// <summary>The deck this card belongs to, from the cascade.</summary>
    [CascadingParameter] public CardDeck Deck { get; set; } = default!;

    /// <summary>The words on the card.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>One line under the title; none, and the card is its title.</summary>
    [Parameter] public string? Summary { get; set; }

    /// <summary>A small mono tag on the card, such as the project's name; none, and the card has no tag.</summary>
    [Parameter] public string? Tag { get; set; }

    /// <summary>The story the deck shows when the card is open.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Tells the deck this card exists, once.</summary>
    protected override void OnInitialized() => Deck.Add(this);
}
