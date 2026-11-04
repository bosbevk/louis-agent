using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components;

public partial class ThemeSwitcher
{
    private sealed record Theme(string Id, string Name);

    private List<Theme> _themes = [];
    private string _current = "retro";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _themes = await Http.GetFromJsonAsync<List<Theme>>("css/themes/themes.json") ?? [];
            _current = await JS.InvokeAsync<string>("louisChat.getTheme");
        }
        catch (Exception)
        {
            // No list, no switcher: the default theme stays.
        }
    }

    private async Task Change(ChangeEventArgs e)
    {
        if (e.Value?.ToString() is not { } id || _themes.All(t => t.Id != id)) return;
        _current = id;
        await JS.InvokeVoidAsync("louisChat.setTheme", id);
    }
}
