using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;

namespace LouisAgent.Packs.Site.Tools.Cards;

/// <summary>
/// What makes choices good cards: a question they answer, two to four of them, each a short label (a button's words,
/// sent back as the visitor's message) of its own, a reason of one line at most, and one suggestion at most. The
/// first thing wrong, in words the model can act on; nothing when the choices are good.
/// </summary>
public static class OfferCheck
{
    /// <summary>The longest label, a button's words.</summary>
    public const int LabelLength = 40;

    /// <summary>The longest reason, one line under the label.</summary>
    public const int WhyLength = 80;

    /// <summary>What's wrong with the choices, or nothing when they make good cards.</summary>
    public static string? Problem(string question, IReadOnlyList<OfferChoice> choices)
    {
        if (string.IsNullOrWhiteSpace(question))
            return MessageText.Format(SiteToolMessage.OfferWithoutQuestion, new { });
        if (choices.Count is < 2 or > 4) return MessageText.Format(SiteToolMessage.OfferCount, new { choices.Count });

        for (int at = 0; at < choices.Count; at++)
            if (ChoiceProblem(choices[at], at + 1) is { } problem)
                return problem;

        int suggested = choices.Count(choice => choice.Suggested);
        if (suggested > 1) return MessageText.Format(SiteToolMessage.OfferSuggestedTwice, new { Count = suggested });

        var twice = choices.GroupBy(choice => choice.Label.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(same => same.Count() > 1);
        return twice is null ? null : MessageText.Format(SiteToolMessage.OfferSameLabel, new { Label = twice.Key });
    }

    /// <summary>A choice's own problem: its label empty or too long, or its reason over a line.</summary>
    private static string? ChoiceProblem(OfferChoice choice, int number)
    {
        string label = choice.Label?.Trim() ?? "";
        if (label.Length is 0 or > LabelLength)
            return MessageText.Format(SiteToolMessage.OfferBadLabel,
                new
                {
                    Number = number,
                    Label = label,
                    label.Length,
                    Most = LabelLength
                });
        if (choice.Why is { } why && why.Trim().Length > WhyLength)
            return MessageText.Format(SiteToolMessage.OfferLongWhy,
                new
                {
                    Number = number,
                    why.Trim().Length,
                    Most = WhyLength
                });
        return null;
    }
}
