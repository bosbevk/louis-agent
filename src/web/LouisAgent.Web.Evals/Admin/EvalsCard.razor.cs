using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Evals.Client;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Admin;

/// <summary>
/// The evals' card on the admin's home: how many evals the host has, and how many of their last runs passed every
/// conversation. It reads the same list the evals' page reads; a refused key is the base's to report.
/// </summary>
public partial class EvalsCard
{
    private EvalsReply? _evals;

    [Inject] private EvalsApi Evals { get; set; } = null!;

    /// <summary>How many evals' last runs passed in full.</summary>
    private int PassedInFull => _evals?.Evals.Count(row => row.LastRunPassedInFull()) ?? 0;

    /// <summary>Reads the host's evals.</summary>
    protected override async Task ReadAsync() => _evals = await Evals.ListAsync();
}
