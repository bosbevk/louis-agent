using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Chats.Calls.Answers;
using Microsoft.AspNetCore.Components;

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

    /// <summary>Every choice until the visitor answers; then the one they chose, if they chose one.</summary>
    private IEnumerable<OfferChoice> Shown => Answer is null
        ? Offer.Choices
        : Offer.Choices.Where(choice => string.Equals(choice.Label.Trim(), Answer.Trim(),
            StringComparison.OrdinalIgnoreCase) || OfferWriting.Answers(Offer.Question, choice, Answer));

    /// <summary>What a chosen card shows: its label, or for a box (FX147) the label and what was written.</summary>
    private string ChosenWords(OfferChoice choice) =>
        OfferWriting.Answers(Offer.Question, choice, Answer!) ? Answer! : choice.Label;
}
