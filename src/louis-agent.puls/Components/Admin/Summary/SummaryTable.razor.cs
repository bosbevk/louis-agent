using LouisAgent.Puls.Admin.Summary;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>One mode's roll-up as a table (F18 step 5c.3).</summary>
public partial class SummaryTable
{
    [Parameter, EditorRequired] public ModeSlices Mode { get; set; } = null!;

    /// <summary>The run the slices are of: its explorer opens a task's conversation.</summary>
    [Parameter, EditorRequired] public string RunId { get; set; } = "";

    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>A bar as wide as the rate behind the number (6a.4); none for a slice too few for a rate. Orange (hat) for pass^k, green for the others, as the pass-rates list colours them.</summary>
    private static RenderFragment? Bar(SliceView slice, double? rate, bool hat) =>
        slice.TooFew || rate is not { } value ? null : builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "rate-track");
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", hat ? "rate-fill hat" : "rate-fill");
            builder.AddAttribute(4, "style", $"width: {Math.Round(value * 100, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)}%");
            builder.CloseElement();
            builder.CloseElement();
        };
}
