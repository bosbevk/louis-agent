using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.CodeReview.Components;

/// <summary>The theme picker: the themes the app ships, and the one this browser last chose.</summary>
public partial class ThemeSwitcher
{
    /// <summary>One line of <c>themes.json</c>.</summary>
    private sealed record Theme(string Id, string Name);

    private List<Theme> _themes = [];
    private string _current = "retro";

    /// <summary>Reads the list of themes and the browser's saved choice.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _themes = await Http.GetFromJsonAsync<List<Theme>>("css/themes/themes.json") ?? [];
            _current = await JS.InvokeAsync<string>("siteTheme.get");
        }
        catch (Exception)
        {
            // No list, no switcher: the default theme stays.
        }
    }

    /// <summary>Applies the chosen theme, as long as it's one we ship.</summary>
    private async Task Change(ChangeEventArgs e)
    {
        if (e.Value?.ToString() is not { } id || _themes.All(t => t.Id != id)) return;
        _current = id;
        await JS.InvokeVoidAsync("siteTheme.set", id);
    }
}
