using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Cards;

/// <summary>
/// A row of short cards and one story under it (F113, Louis: "make the card a short summary, and clickable with the
/// detail sliding in at the bottom"). The cards inside register themselves; the deck draws them and keeps which one is
/// open.
/// </summary>
/// <remarks>
/// The tab-set pattern: children declare what they are through a cascading value and the parent draws them, so a
/// page writes titles, summaries and stories and never any state. CSS does the slide, so the deck needs no script.
/// The same on every site, so it is the framework's (ADR 0084).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class CardDeck
{
    private readonly List<Card> _cards = [];

    /// <summary>The cards, as <see cref="Card"/> components.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>How many cards stand in a row on a wide screen; a phone stacks them.</summary>
    [Parameter] public int Columns { get; set; } = 3;

    private IReadOnlyList<Card> Cards => _cards;

    private Card? Open { get; set; }

    /// <summary>A card announces itself as it initialises; the deck redraws with it in the row.</summary>
    internal void Add(Card card)
    {
        if (_cards.Contains(card)) return;
        _cards.Add(card);
        StateHasChanged();
    }

    private bool IsOpen(Card card) => ReferenceEquals(Open, card);

    /// <summary>A click opens the card, or closes it when it was the open one.</summary>
    private void Toggle(Card card) => Open = IsOpen(card) ? null : card;
}
