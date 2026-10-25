using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// What this instance is and has: its health, the price table and exchange rates it loaded (public, for other hosts
/// and the web app), the toolsets a session can load, the model, the roles and the LLM profiles.
/// </summary>
internal static class InfoEndpoints
{
    public static void MapInfo(this IEndpointRouteBuilder routes, AgentHostContext host)
    {
        routes.MapGet("/health", () => Results.Json(new { status = "ok" }));

        // The price table this API's usage ledger prices with, for other hosts' PRICES_URL (the orchestrator in the demo). Public
        // like /health: prices aren't secret, and a host fetches them without the API key. 404 when there is no table, so a
        // caller falls back to its own file.
        routes.MapGet("/prices", () => host.Prices.IsEmpty
            ? Results.Json(Error("not_found_error", MessageText.Format(ApiUserText.NoPriceTable)),
                statusCode: StatusCodes.Status404NotFound)
            : Results.Json(host.Prices, PriceTable.Json));

        // The exchange rates this API loaded (F14), in our rates.json format, for other hosts' RATES_URL (with
        // RATES_FORMAT=rates-json; the demo's orchestrator) and the web app. Public like /prices; 404 when it has none.
        routes.MapGet("/rates", () => host.Rates.IsEmpty
            ? Results.Json(Error("not_found_error", MessageText.Format(ApiUserText.NoRates)),
                statusCode: StatusCodes.Status404NotFound)
            : Results.Text(host.Rates.ToJson(), "application/json"));

        // Worked out once: looking a symbol up goes through every culture .NET knows.
        var Symbols = new Lazy<Dictionary<string, string>>(() =>
            host.Rates.Rates.Keys.Append(host.Prices.Currency).Distinct().Order(StringComparer.Ordinal)
                .ToDictionary(code => code, code => DisplayCurrency.DistinctSymbol(code, DisplayCurrency.SymbolFor(code), host.Prices.Currency)));

        // What the web app needs to show costs in another currency (F14): the server's display currency (from HOST_CULTURE or
        // DISPLAY_CURRENCY; the browser's own locale is the display language, not the region) and a symbol for every currency
        // in /rates, worked out here, since the browser's .NET carries little culture data. Public like /rates.
        routes.MapGet("/rates/display", () => Results.Json(new
        {
            display_currency = host.Display.Code,
            display_symbol = host.Display.Symbol,
            @base = host.Display.Base,
            rate_as_of = host.Display.RateAsOf ?? host.Rates.AsOf,
            symbols = Symbols.Value,
        }));

        // The toolsets a session can load (F5-S3): names, what each is for and whether this instance has its settings. Never
        // the tool definitions, so a general agent can choose without loading them.
        routes.MapGet("/toolsets", (AgentEngineFactory engines) => Results.Json(new
        {
            @default = AgentSessions.DefaultToolsets,
            toolsets = engines.Catalog.Toolsets.Select(t => new
            {
                name = t.Name, description = t.Description, available = t.IsAvailable(engines.Context),
                missing_settings = t.MissingSettings(engines.Context),
            }),
        }));

        // The model this instance answers with (F20: Puls's model picker shows it).
        routes.MapGet("/model", () => Results.Json(new { provider = host.Llm.Provider, model = host.Llm.Model }));

        // The roles a session can choose, per level: GET /roles, and GET /personalities for a client one deploy behind (CL14).
        foreach (string route in RolesView.Routes)
            routes.MapGet(route, (AgentEngineFactory engines) => Results.Json(RolesView.Of(engines)));

        // The LLM profiles a session can name besides this instance's model (F30): null is the instance's own.
        routes.MapGet("/llm-profiles", (AgentEngineFactory engines) => Results.Json(new
        {
            @default = (string?)null,
            profiles = engines.SessionLlms,
        }));
    }

    private static object Error(string type, string message) => ApiErrors.Body(type, message);
}
