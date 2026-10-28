using LouisAgent.Api.Contracts.Admin.Checks;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Head;

/// <summary>The banner that says why no run can start: there's no store, or it doesn't answer.</summary>
public partial class StoreBanner
{
    /// <summary>The store's state.</summary>
    [Parameter, EditorRequired]
    public StoreRow Store { get; set; } = null!;
}
