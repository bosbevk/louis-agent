using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components;

/// <summary>The currency picker: choosing one converts every cost from then on.</summary>
public partial class CurrencySwitcher : IDisposable
{
    /// <summary>Listens for changes, then loads the rates.</summary>
    protected override async Task OnInitializedAsync()
    {
        Currency.Changed += Redraw;
        await Currency.LoadAsync();
    }

    /// <summary>Whatever changed the currency, draw it again.</summary>
    private void Redraw() => InvokeAsync(StateHasChanged);

    /// <summary>Applies the chosen currency.</summary>
    private async Task Change(ChangeEventArgs e)
    {
        if (e.Value?.ToString() is { } chosen) await Currency.ChooseAsync(chosen);
    }

    /// <summary>Stops listening when the picker goes.</summary>
    public void Dispose() => Currency.Changed -= Redraw;
}
