using LouisAgent.Web.Shared.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Head;

/// <summary>
/// Says what went wrong. When the API wants the portal's key, it asks for it and keeps it in this browser, then tells
/// the page to read again.
/// </summary>
public partial class AdminGate
{
    private string _keyDraft = "";

    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>What went wrong.</summary>
    [Parameter, EditorRequired]
    public string Error { get; set; } = "";

    /// <summary>The API refused the key, so the box to enter one is shown.</summary>
    [Parameter]
    public bool NeedsKey { get; set; }

    /// <summary>A key was saved; the page reads again with it.</summary>
    [Parameter]
    public EventCallback OnSaved { get; set; }

    /// <summary>Keeps the key typed in this browser, empties the box, and lets the page read again.</summary>
    private async Task SaveKeyAsync()
    {
        await Store.SaveAdminKeyAsync(_keyDraft);
        _keyDraft = "";
        await OnSaved.InvokeAsync();
    }
}
