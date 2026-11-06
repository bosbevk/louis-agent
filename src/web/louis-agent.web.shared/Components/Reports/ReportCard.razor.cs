using LouisAgent.Api.Contracts.Admin.Reports;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Shared.Components.Reports;

/// <summary>
/// Shows one report as the API gave it (F35.7): the table is the pack's, and this adds no number of its own. It knows
/// no pack, so any web app can show any pack's report with it.
/// </summary>
public partial class ReportCard
{
    /// <summary>The report to show.</summary>
    [Parameter, EditorRequired] public ReportReply Report { get; set; } = null!;
}
