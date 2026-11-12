using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;

namespace LouisAgent.Packs.Site.Tools.Forms;

/// <summary>
/// What makes a good form: a question it asks, one to four fields, each with a short label of its own over a box the
/// form can draw. The first thing wrong, in words the model can act on; nothing when the form is good.
/// </summary>
/// <remarks>
/// The same guard clauses as <c>OfferCheck</c>, one per rule from the top, so a refusal names one thing to fix and the
/// model's second try is the right one.
/// </remarks>
public static class FormCheck
{
    /// <summary>The longest label, the words over a box.</summary>
    public const int LabelLength = 30;

    /// <summary>What's wrong with the form, or nothing when it is good.</summary>
    public static string? Problem(string question, IReadOnlyList<FormField> fields)
    {
        if (string.IsNullOrWhiteSpace(question))
            return MessageText.Format(SiteToolMessage.AskWithoutQuestion, new { });
        if (fields.Count is < 1 or > 4) return MessageText.Format(SiteToolMessage.AskFieldCount, new { fields.Count });

        for (int at = 0; at < fields.Count; at++)
            if (FieldProblem(fields[at], at + 1) is { } problem)
                return problem;

        var twice = fields.GroupBy(field => field.Label.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(same => same.Count() > 1);
        return twice is null ? null : MessageText.Format(SiteToolMessage.AskSameLabel, new { Label = twice.Key });
    }

    /// <summary>A field's own problem: its label empty or too long, or a kind the form can't draw.</summary>
    private static string? FieldProblem(FormField field, int number)
    {
        string label = field.Label?.Trim() ?? "";
        if (label.Length is 0 or > LabelLength)
            return MessageText.Format(SiteToolMessage.AskBadLabel,
                new
                {
                    Number = number,
                    Label = label,
                    label.Length,
                    Most = LabelLength
                });
        if (!Enum.IsDefined(field.Kind))
            return MessageText.Format(SiteToolMessage.AskUnknownKind,
                new
                {
                    Number = number,
                    Kinds = string.Join(", ", Enum.GetNames<FieldKind>())
                });
        return null;
    }
}
