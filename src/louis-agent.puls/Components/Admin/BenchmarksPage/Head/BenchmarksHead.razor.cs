using LouisAgent.Api.Contracts.Admin.Checks;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Head;

/// <summary>
/// The top of the Benchmarks page: its name, a pill that says whether the store answers, and a Reload button.
/// </summary>
public partial class BenchmarksHead
{
    /// <summary>The store's state, once the list is read; no pill before that.</summary>
    [Parameter]
    public StoreRow? Store { get; set; }

    /// <summary>The page is reading, so Reload waits.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Reload was clicked.</summary>
    [Parameter]
    public EventCallback OnReload { get; set; }
}
