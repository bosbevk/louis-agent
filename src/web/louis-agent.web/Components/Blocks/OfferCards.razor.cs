using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Blocks;

/// <summary>
/// Draws the choices the agent offered as cards. A click sends the card's label as the visitor's message; once
/// the visitor has answered, only the card they chose is left.
/// </summary>
/// <remarks>
/// State flows down and the click goes up (the hosts guide's rule): the cards hold no runner, and the chat that owns
/// one says what a choice does through <see cref="Chosen"/>, so a recording, which sends nothing, draws them waiting.
/// </remarks>
public partial class OfferCards
{
    /// <summary>The question and its choices.</summary>
    [Parameter, EditorRequired]
    public Offered Offer { get; set; } = default!;

    /// <summary>The visitor's next message, once there is one: a card's label, or their own words.</summary>
    [Parameter]
    public string? Answer { get; set; }

    /// <summary>Whether the answer that offers these is still coming in.</summary>
    [Parameter]
    public bool Streaming { get; set; }

    /// <summary>What a click does with the label: the chat sends it.</summary>
    [Parameter]
    public EventCallback<string> Chosen { get; set; }

    /// <summary>The cards can't be clicked while the answer streams, or where nothing would send the label.</summary>
    private bool Waiting => Streaming || !Chosen.HasDelegate;

    /// <summary>
    /// Whether these cards are a guide's, which may offer a business live (FX133): the guide's widget says so; a
    /// business's own window, whose cards book and send, never does.
    /// </summary>
    [CascadingParameter(Name = OffersLiveName)]
    public bool OffersLive { get; set; }

    /// <summary>The name the guide's widget cascades <see cref="OffersLive"/> under.</summary>
    public const string OffersLiveName = "OffersLive";

    [Inject] private ChatText Words { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>Every choice until the visitor answers; then the one they chose, if they chose one.</summary>
    private IEnumerable<OfferChoice> Shown => Answer is null
        ? Choices
        : Choices.Where(choice => string.Equals(choice.Label.Trim(), Answer.Trim(),
            StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The agent's choices, and Ask it live where a business's window shows on the page and it isn't offered yet
    /// (FX133): told in the prompt, the guide kept offering only the walkthrough's three, so the card is drawn in code.
    /// </summary>
    private IReadOnlyList<OfferChoice> Choices =>
        OffersLive && Services.GetService<AgentWindows>() is { Any: true } &&
        !Offer.Choices.Any(choice => string.Equals(choice.Label, Words.AskItLive, StringComparison.OrdinalIgnoreCase))
            ? [.. Offer.Choices, new OfferChoice(Words.AskItLive, Words.AskItLiveWhy)]
            : Offer.Choices;
}
