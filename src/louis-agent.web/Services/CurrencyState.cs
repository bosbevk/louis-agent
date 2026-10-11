using Microsoft.JSInterop;

namespace louis_agent.web.Services;

/// <summary>
/// The currency this browser shows costs in (F14). By default the server's: the PC's region, sent by the API, since the
/// browser's own locale is the display language (en-US), not the region. A choice made in the sidebar is kept in this
/// browser, like the theme. Without the API's rates, costs are shown as charged.
/// </summary>
public sealed class CurrencyState(AgentApi api, IJSRuntime js)
{
    private const string ChoiceKey = "louis-agent.currency";

    private RatesTable? _rates;
    private RatesDisplay? _display;
    private bool _loading;

    /// <summary>The currency costs are shown in; null until the rates have loaded (then costs show as charged).</summary>
    public CurrencyChoice? Current { get; private set; }

    /// <summary>The currencies to choose from: the charged one, the server's, then the rest by code.</summary>
    public IReadOnlyList<(string Code, string Symbol)> Choices { get; private set; } = [];

    /// <summary>Raised when the currency changes, so every view re-draws its costs.</summary>
    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (_loading || _rates is not null) return;
        _loading = true;
        try
        {
            _rates = await api.GetRatesAsync();
            _display = await api.GetRatesDisplayAsync();
        }
        catch (Exception)
        {
            // An older API, or no rates on the server: costs stay as charged.
            _rates = null;
            return;
        }
        finally
        {
            _loading = false;
        }

        Choices = _rates.Rates.Keys.Append(_rates.Base)
            .Distinct()
            .OrderBy(code => code == _rates.Base ? 0 : code == _display.DisplayCurrency ? 1 : 2)
            .ThenBy(code => code, StringComparer.Ordinal)
            .Select(code => (code, SymbolOf(code)))
            .ToList();

        string? saved = null;
        try { saved = await js.InvokeAsync<string?>("localStorage.getItem", ChoiceKey); }
        catch (JSException) { }
        Select(saved is not null && IsKnown(saved) ? saved : _display.DisplayCurrency);
    }

    /// <summary>Shows costs in <paramref name="code"/> from now on, in this browser.</summary>
    public async Task ChooseAsync(string code)
    {
        if (!IsKnown(code)) return;
        Select(code);
        try { await js.InvokeVoidAsync("localStorage.setItem", ChoiceKey, code); }
        catch (JSException) { }
    }

    private bool IsKnown(string code) => _rates is not null && (code == _rates.Base || _rates.Rates.ContainsKey(code));

    private void Select(string code)
    {
        if (_rates is null) return;
        decimal rate = code == _rates.Base ? 1m : _rates.Rates.GetValueOrDefault(code, 0m);
        if (rate <= 0) code = _rates.Base;
        Current = new CurrencyChoice(code, SymbolOf(code), code == _rates.Base ? 1m : rate,
            code == _rates.Base ? null : _rates.AsOf, _rates.Base);
        Changed?.Invoke();
    }

    private string SymbolOf(string code) => _display?.Symbols.GetValueOrDefault(code) ?? code;
}