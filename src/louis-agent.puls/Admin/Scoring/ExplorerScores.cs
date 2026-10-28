using System.Text.Json;
using LouisAgent.Puls.Admin.Explorer.Tree;
using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Admin.Scoring;

/// <summary>
/// What the explorer reads from a conversation's scores: whether it was routed right, whether it got a real answer,
/// its chips, and per mode how many ingredients were priced right and how many meals came without a list.
/// </summary>
/// <remarks>
/// The scores are the one part of a conversation whose shape each eval decides, so the contract keeps them as JSON
/// and this is the one place in the explorer that reads their names; a name an eval doesn't send reads as nothing.
/// The chips are <c>ScoreChips</c>' shared ones, so the explorer and the live table can't drift apart (the unattended
/// run's finding 11). Step 7c moves these to grades and tracked metrics.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class ExplorerScores
{
    /// <summary>The eval scored the conversation as sent where it should go.</summary>
    public static bool RoutedRight(ConversationNode c) =>
        c.Scores.ValueKind == JsonValueKind.Object && c.Scores.TryGetProperty("routed_right", out var v)
                                                   && v.ValueKind == JsonValueKind.True;

    /// <summary>How many of a mode's conversations were routed right.</summary>
    public static int RoutedRight(ModeNode m) => m.Conversations.Count(RoutedRight);

    /// <summary>
    /// The conversation got a real answer: routed right and, where the check lists ingredients, at least 80% of them
    /// priced with their source. A meal that came without a list is no answer.
    /// </summary>
    /// <remarks>Guard clauses (Fowler's <i>Replace Nested Conditional with Guard Clauses</i>), one case each.</remarks>
    public static bool Answered(ConversationNode c)
    {
        if (!RoutedRight(c)) return false;
        if (!c.Scores.TryGetProperty("ingredients", out var n)) return true;
        if (n.ValueKind != JsonValueKind.Number) return !c.Task.StartsWith("cook");
        return c.Scores.TryGetProperty("priced_ratio", out var r) && r.ValueKind == JsonValueKind.Number
                                                                  && r.GetDouble() >= 0.8;
    }

    /// <summary>
    /// The conversation's chips: the scores' own (the live run's table shows the same), then its errors.
    /// </summary>
    public static IReadOnlyList<PricedChip> Chips(ConversationNode c)
    {
        if (c.Scores.ValueKind != JsonValueKind.Object) return [];

        var chips = ScoreChips.Common(c.Role, c.Scores).ToList();
        if (c.Errors > 0) chips.Add(new PricedChip($"errors {c.Errors}", "bad"));
        return chips;
    }

    /// <summary>
    /// A mode's ingredients priced right, out of those listed, over its conversations; nothing where the check
    /// counts none. Right is priced with its source, or rightly left unpriced in a town with no shop.
    /// </summary>
    public static AccuracyRing? Accuracy(ModeNode m)
    {
        var listed = m.Conversations.Where(c => Number(c.Scores, "ingredients") is not null).ToList();
        int items = listed.Sum(c => Number(c.Scores, "ingredients") ?? 0);
        if (items == 0) return null;

        return AccuracyRing.Of(Sum(listed, "priced"), Sum(listed, "no_shop_rows"), Sum(listed, "unsourced"),
            Sum(listed, "estimated"), items);
    }

    /// <summary>How many of a mode's meals came back without the ingredient list the check expected.</summary>
    public static int Unlisted(ModeNode m) =>
        m.Conversations.Count(c => c.Task.StartsWith("cook") && c.Scores.ValueKind == JsonValueKind.Object
                                                             && c.Scores.TryGetProperty("ingredients", out var n)
                                                             && n.ValueKind == JsonValueKind.Null);

    /// <summary>One score added up over some conversations, a missing one as zero.</summary>
    private static int Sum(IEnumerable<ConversationNode> conversations, string name) =>
        conversations.Sum(c => Number(c.Scores, name) ?? 0);

    /// <summary>A whole-number score, when the scores have it as a number.</summary>
    private static int? Number(JsonElement scores, string name) =>
        scores.ValueKind == JsonValueKind.Object && scores.TryGetProperty(name, out var v)
                                                 && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : null;
}
