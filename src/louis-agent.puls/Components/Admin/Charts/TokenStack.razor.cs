using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Charts;

/// <summary>The four token kinds of some requests as one stacked bar (F18 step 6a.3b).</summary>
public partial class TokenStack
{
    /// <summary>The four counts, in <see cref="LouisAgent.Puls.Admin.Charts.TokenKinds"/>' order: input, cache written, cache read, output.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<long> Parts { get; set; } = [0, 0, 0, 0];

    /// <summary>The count the bar's full width stands for, shared by its siblings so they compare.</summary>
    [Parameter, EditorRequired] public long Top { get; set; }
}
