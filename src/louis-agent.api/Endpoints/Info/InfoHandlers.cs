using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Info;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Pricing.Rates;
using Microsoft.AspNetCore.Http.HttpResults;
using PricesAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Core.Pricing.Prices.PriceTable>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using RatesAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Info;

/// <summary>
/// What this instance is and has, as named methods: its health, the price table and exchange rates it loaded, its
/// display currency, the toolsets a session can load, its model, its roles and its LLM profiles.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour: the routes share the host the agent was
/// built as (its prices, rates, display currency and model), so it lives here and each route is a named method a test
/// can call with no HTTP. Each returns <c>TypedResults</c> with its answers declared; the info replies are written with
/// <c>InfoJson.SnakeCase</c>, the price table with its own options, and the rates as the text of our rates.json.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class InfoHandlers(AgentHostContext host)
{
    // Worked out once: looking a symbol up goes through every culture .NET knows.
    private readonly Lazy<Dictionary<string, string>> _symbols = new(() => SymbolsOf(host));

    /// <summary>The probe: the instance is up.</summary>
    public static JsonHttpResult<HealthReply> GetHealth() =>
        TypedResults.Json(new HealthReply("ok"), InfoJson.SnakeCase);

    /// <summary>
    /// The price table this API's usage ledger prices with, for other hosts' <c>PRICES_URL</c>; a 404 when there is
    /// none, so a caller falls back to its own file.
    /// </summary>
    public PricesAnswer GetPrices()
    {
        if (host.Prices.IsEmpty) return NotFound(MessageText.Format(ApiUserText.NoPriceTable));
        return TypedResults.Json(host.Prices, PriceTable.Json);
    }

    /// <summary>
    /// The exchange rates this API loaded, in our rates.json format, for other hosts' <c>RATES_URL</c> and the web
    /// app; a 404 when it has none.
    /// </summary>
    public RatesAnswer GetRates()
    {
        if (host.Rates.IsEmpty) return NotFound(MessageText.Format(ApiUserText.NoRates));
        return TypedResults.Text(host.Rates.ToJson(), "application/json");
    }

    /// <summary>
    /// What the web app needs to show costs in another currency: the server's display currency and a symbol for every
    /// currency in the rates, worked out here since the browser's .NET carries little culture data.
    /// </summary>
    public JsonHttpResult<DisplayReply> GetDisplay()
    {
        var display = host.Display;
        var reply = new DisplayReply(display.Code, display.Symbol, display.Base, display.RateAsOf ?? host.Rates.AsOf,
            _symbols.Value);
        return TypedResults.Json(reply, InfoJson.SnakeCase);
    }

    /// <summary>
    /// The toolsets a session can load: what each is for, and whether this instance has its settings.
    /// </summary>
    public JsonHttpResult<ToolsetsReply> GetToolsets(AgentEngineFactory engines)
    {
        var toolsets = engines.Catalog.Toolsets.Select(t => ToolsetRow.Of(t, engines.Context)).ToList();
        return TypedResults.Json(new ToolsetsReply(AgentSessions.DefaultToolsets, toolsets), InfoJson.SnakeCase);
    }

    /// <summary>The model this instance answers with.</summary>
    public JsonHttpResult<ModelReply> GetModel() => TypedResults.Json(ModelReply.Of(host.Llm), InfoJson.SnakeCase);

    /// <summary>The roles a session can choose, per level.</summary>
    public JsonHttpResult<RolesReply> GetRoles(AgentEngineFactory engines) =>
        TypedResults.Json(RolesReply.Of(engines), InfoJson.SnakeCase);

    /// <summary>The LLM profiles a session can name besides this instance's model.</summary>
    public JsonHttpResult<LlmProfilesReply> GetLlmProfiles(AgentEngineFactory engines) =>
        TypedResults.Json(new LlmProfilesReply(null, engines.SessionLlms), InfoJson.SnakeCase);

    /// <summary>A symbol for each currency the rates and the price table name, told apart from the base's.</summary>
    private static Dictionary<string, string> SymbolsOf(AgentHostContext host) =>
        host.Rates.Rates.Keys
            .Append(host.Prices.Currency)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToDictionary(code => code,
                code => DisplayCurrency.DistinctSymbol(code, DisplayCurrency.SymbolFor(code), host.Prices.Currency));

    /// <summary>A 404 with its reason.</summary>
    private static JsonHttpResult<ApiError> NotFound(string message) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", message);
}
