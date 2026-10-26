using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Components.Admin.Reviews;

/// <summary>How often each reviewed judge agreed with the people, and the download of their labels (F18 step 6a.3).</summary>
public partial class AgreementPanel
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    [Parameter, EditorRequired] public JsonElement Run { get; set; }
    [Parameter, EditorRequired] public string RunId { get; set; } = "";

    private string? _error;

    private IReadOnlyList<JudgeAgreement> Judges => AgreementView.Of(Run);

    /// <summary>The bar's width: the share the judge agreed, an empty bar for a class with no labels.</summary>
    private static string Width(ClassAgreement c) => c.Total == 0 ? "0%" : $"{(int)Math.Round(100.0 * c.Agreed / c.Total, MidpointRounding.AwayFromZero)}%";

    /// <summary>The 2x2 counts behind a bar, on hover.</summary>
    private static string Hover(string you, ClassAgreement c) =>
        $"Of the {c.Total} answers you {you}: the judge passed {c.JudgePass}, failed {c.JudgeFail}, couldn't tell {c.JudgeUnknown}.";

    private async Task DownloadAsync(string grader)
    {
        _error = null;
        try
        {
            byte[] zip = await Api.GetBytesAsync($"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/labels?grader={Uri.EscapeDataString(grader)}");
            await Js.InvokeVoidAsync("louisDownload", $"{grader}-labels-{RunId}.zip", Convert.ToBase64String(zip), "application/zip");
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }
}
