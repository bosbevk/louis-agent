using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage;

/// <summary>An eval's runs as a table, each linking to its own page.</summary>
public partial class EvalRunTable
{
    /// <summary>The runs, newest first, as the API listed them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<EvalRunRow> Runs { get; set; } = [];

    /// <summary>The browser's currency for the costs.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
