using System.Text.Json;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>The trend per category, per model (F18 step 5c.4): a chart for each category the trend carries, drawn with the trend chart showing only the pass rates.</summary>
public partial class CategoryTrends
{
    /// <summary>The trend as <c>GET /v1/admin/benchmarks/checks/{check}/trend</c> answers it: its <c>categories</c>, each with its lines under <c>modes</c>.</summary>
    [Parameter, EditorRequired] public JsonElement Trend { get; set; }

    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>The run whose page this is, marked on each chart.</summary>
    [Parameter] public string? Highlight { get; set; }

    /// <summary>A category with no rate in any run has no chart, and the API sends none; a trend from before categories has none at all.</summary>
    private sealed record Category(string Name, JsonElement Chart);

    /// <summary>The panels a category's chart shows: the pass rates only, since a category has no cost or token trend of its own.</summary>
    private static readonly IReadOnlyList<string> Panels = ["passat", "passhat"];

    private IReadOnlyList<Category> Categories =>
        Trend.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array
            ? [.. categories.EnumerateArray().Select(category => new Category(category.GetProperty("category").GetString() ?? "", category))]
            : [];
}
