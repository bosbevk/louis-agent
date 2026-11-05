using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared.Components;

public partial class CurrencySwitcher : IDisposable
{
    protected override async Task OnInitializedAsync()
    {
        Currency.Changed += Redraw;
        await Currency.LoadAsync();
    }

    private void Redraw() => InvokeAsync(StateHasChanged);

    private async Task Change(ChangeEventArgs e)
    {
        if (e.Value?.ToString() is { } chosen) await Currency.ChooseAsync(chosen);
    }

    public void Dispose() => Currency.Changed -= Redraw;
}
