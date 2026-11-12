using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Chats.Calls.Answers;

/// <summary>
/// Whether the visitor's message answers a card the turn before drew (FX142): a choice it offered, clicked, or a form
/// it asked with, sent. The folded card already shows that answer, so a chat may leave its bubble out.
/// </summary>
public static class CardAnswer
{
    /// <summary>Whether <paramref name="message"/> is a click on, or the sending of, a card in <paramref name="before"/>.</summary>
    public static bool Answers(Turn before, string message) => before.Blocks.Any(block => Answers(block, message));

    /// <summary>Whether the message is one of this block's choices, or its filled form.</summary>
    private static bool Answers(Block block, string message)
    {
        if (OfferCall.Of(block) is { } offered)
            return offered.Choices.Any(choice =>
                string.Equals(choice.Label.Trim(), message.Trim(), StringComparison.OrdinalIgnoreCase));
        if (AskForCall.Of(block) is { } form) return FormAnswer.Read(form, message) is not null;
        return false;
    }
}
