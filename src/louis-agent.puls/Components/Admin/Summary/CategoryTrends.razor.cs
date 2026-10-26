using System.Globalization;
using System.Text.Json;
using LouisAgent.Puls.Admin.Summary;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>The trend per category, per model (F18 step 5c.4), as a card per category: the latest numbers to read, and the trend lines once there are two runs to join.</summary>
public partial class CategoryTrends
{
    /// <summary>The trend as <c>GET /v1/admin/benchmarks/checks/{check}/trend</c> answers it: its <c>categories</c>, each with its lines under <c>modes</c>.</summary>
    [Parameter, EditorRequired] public JsonElement Trend { get; set; }

    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The run whose page this is, marked on each chart.</summary>
    [Parameter] public string? Highlight { get; set; }

    /// <summary>The panels a category's chart shows: the pass rates only, since a category has no cost or token trend of its own.</summary>
    private static readonly IReadOnlyList<string> Panels = ["passat", "passhat"];

    /// <summary>The trend chart's palette, so a mode's bar is the colour of its line.</summary>
    private static readonly string[] Palette = ["#3987e5", "#d95926", "#199e70", "#c98500", "#d55181", "#008300"];

    private static string Colour(int i) => Palette[i % Palette.Length];

    private IReadOnlyList<CategoryCard> Cards =>
        Trend.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array
            ? [.. categories.EnumerateArray().Select(CategoryTrendReader.Read)]
            : [];

    private static string Percent(double? rate) => rate is { } value ? value.ToString("0%", CultureInfo.InvariantCulture) : "–";

    private static string Width(double? rate) => rate is { } value ? Math.Round(value * 100, 1).ToString(CultureInfo.InvariantCulture) + "%" : "0";
}
