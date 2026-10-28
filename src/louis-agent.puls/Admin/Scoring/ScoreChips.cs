using System.Text.Json;
using LouisAgent.Puls.Admin.Images;
using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Admin.Scoring;

/// <summary>
/// A conversation's scores as the chips under its turns: who answered and whether that was the right one, how the
/// follow-up went, then what the checks share (the shopping list's prices, the deals, the shops and the errors).
/// </summary>
/// <remarks>
/// The scores are the one part of a conversation whose shape each eval decides, so the contract keeps them as JSON and
/// this class reads the names it knows; a name an eval doesn't send simply shows no chip.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class ScoreChips
{
    /// <summary>The chips for one conversation, in the order the table shows them.</summary>
    public static IReadOnlyList<PricedChip> Of(string? role, JsonElement scores) =>
        [.. Shared(role, scores), .. LiveOnly(scores)];

    /// <summary>
    /// The chips every view shares, without the live table's own last two (the shops read and the errors counted in
    /// the scores): the run explorer adds its conversation's errors instead.
    /// </summary>
    public static IReadOnlyList<PricedChip> Common(string? role, JsonElement scores) => [.. Shared(role, scores)];

    /// <summary>The live table's own chips: how many shops were read, and the errors the scores counted.</summary>
    private static IEnumerable<PricedChip> LiveOnly(JsonElement s)
    {
        if (s.TryGetProperty("shop_hosts", out var shops) && shops.ValueKind == JsonValueKind.Array)
            yield return new PricedChip($"shops {shops.GetArrayLength()}", "");
        if (Int(s, "errors") is > 0 and var errors) yield return new PricedChip($"foute {errors}", "bad");
    }

    /// <summary>Each chip every view shows, one after the other.</summary>
    private static IEnumerable<PricedChip> Shared(string? role, JsonElement s)
    {
        if (s.TryGetProperty("routed_right", out var right))
        {
            bool ok = right.ValueKind == JsonValueKind.True;
            yield return new PricedChip($"{role ?? "Puls itself"} {(ok ? "✓" : "✗")}", ok ? "ok" : "bad");
        }

        if (Str(s, "follow_up") is { } follow) yield return new PricedChip(follow, follow == "continued" ? "ok" : "");
        foreach (var chip in ShoppingChips(s)) yield return chip;

        // F34: deals named beside a price; a shop priced with no tool for it (Louis: only shops with a tool) is wrong.
        if (Int(s, "deals_named") is > 0 and var deals) yield return new PricedChip($"deals {deals}", "ok");
        if (Int(s, "untooled_shops") is > 0 and var untooled)
            yield return new PricedChip($"no-tool shops {untooled}", "bad");

        // A town with no shop (Louis: "no shop there mos?"): no price is right, a price is from a store that isn't the
        // town's.
        if (Int(s, "no_shop_rows") is > 0 and var noShop)
            yield return new PricedChip($"no shop, no price {noShop}", "ok");
        if (Int(s, "wrong_store") is > 0 and var wrong) yield return new PricedChip($"wrong store {wrong}", "bad");
        if (s.TryGetProperty("no_catalogue_line", out var none) && none.ValueKind == JsonValueKind.True)
            yield return new PricedChip("no catalogue here", "");

        // Louis: "how can we verify … that the address was used": the searches the town's store answered, and the
        // default's.
        if (Int(s, "store_for_area") is > 0 and var local) yield return new PricedChip($"town's store {local}", "ok");
        if (Int(s, "store_default") is > 0 and var fallback)
            yield return new PricedChip($"default store {fallback}", "warn");
    }

    /// <summary>
    /// The shopping list's chips, when the conversation had one: priced, the photo, guessed and per-kg prices.
    /// </summary>
    private static IEnumerable<PricedChip> ShoppingChips(JsonElement s)
    {
        if (Int(s, "ingredients") is not { } items) yield break;

        // FX24: an estimate counts as priced (the cook's answer while the stores' prices are awaited), said apart.
        decimal ratio = Dec(s, "priced_ratio") ?? 0;
        foreach (var chip in PricedChips.Of(Int(s, "priced") ?? 0, Int(s, "estimated"), items, ratio))
            yield return chip;
        if (ImageChip.Of(Int(s, "images")) is { } image) yield return image;
        if (Int(s, "unsourced") is > 0 and var guessed) yield return new PricedChip($"guessed {guessed}", "warn");

        // A price per kg given as a pack's (review of PR #58): sourced, but not what the pack costs.
        if (Int(s, "per_kg") is > 0 and var perKg) yield return new PricedChip($"per kg {perKg}", "warn");
    }

    /// <summary>A text score; a number is shown as it was sent.</summary>
    private static string? Str(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null
        };
    }

    /// <summary>A whole-number score, when it is one.</summary>
    private static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n)
            ? n
            : null;

    /// <summary>A decimal score, when it is a number.</summary>
    private static decimal? Dec(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;
}
