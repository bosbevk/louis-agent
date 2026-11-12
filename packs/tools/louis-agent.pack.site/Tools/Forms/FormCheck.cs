using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;
using LouisAgent.Packs.Site.Messages.Holes.Forms;

namespace LouisAgent.Packs.Site.Tools.Forms;

/// <summary>
/// What makes a good form: a question it asks, one to four fields, each with a short label of its own over a box the
/// form can draw. The first thing wrong, in words the model can act on; nothing when the form is good.
/// </summary>
/// <remarks>
/// The same guard clauses as <c>OfferCheck</c>, one per rule from the top, so a refusal names one thing to fix and the
/// model's second try is the right one.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
public static class FormCheck
{
    /// <summary>The longest label, the words over a box.</summary>
    public const int LabelLength = 30;

    /// <summary>What's wrong with the form, or nothing when it is good.</summary>
    public static string? Problem(string question, IReadOnlyList<FormField> fields)
    {
        if (string.IsNullOrWhiteSpace(question)) return MessageText.Format(SiteToolMessage.AskWithoutQuestion);
        if (fields.Count is < 1 or > 4)
        {
            var fieldCount = new AskFieldCountHoles(fields.Count);
            return MessageText.Format(SiteToolMessage.AskFieldCount, fieldCount);
        }

        for (int at = 0; at < fields.Count; at++)
            if (FieldProblem(fields[at], at + 1) is { } problem)
                return problem;

        var twice = fields.GroupBy(field => field.Label.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(same => same.Count() > 1);
        if (twice is null) return null;
        var sameLabel = new AskSameLabelHoles(twice.Key);
        return MessageText.Format(SiteToolMessage.AskSameLabel, sameLabel);
    }

    /// <summary>A field's own problem: its label empty or too long, or a kind the form can't draw.</summary>
    private static string? FieldProblem(FormField field, int number)
    {
        string label = field.Label?.Trim() ?? "";
        if (label.Length is 0 or > LabelLength)
        {
            var badLabel = new AskBadLabelHoles(number, label, label.Length, LabelLength);
            return MessageText.Format(SiteToolMessage.AskBadLabel, badLabel);
        }

        if (Enum.IsDefined(field.Kind)) return null;
        var unknownKind = new AskUnknownKindHoles(number, string.Join(", ", Enum.GetNames<FieldKind>()));
        return MessageText.Format(SiteToolMessage.AskUnknownKind, unknownKind);
    }
}
