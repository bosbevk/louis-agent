using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Puls.Components.Admin.Reviews;

/// <summary>
/// How often each reviewed judge agreed with the people, and the download of their labels (F18 step 6a.3).
/// </summary>
public partial class AgreementPanel
{
    /// <summary>The API the labels come from.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The browser, which saves the labels zip.</summary>
    [Inject]
    private IJSRuntime Js { get; set; } = null!;

    /// <summary>The run's agreement rows as the contract's stored run reply carries them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<AgreementRow> Agreement { get; set; } = [];

    /// <summary>The run they belong to.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>What went wrong with a download, for the panel to say.</summary>
    private string? _error;

    /// <summary>Each judge and how often it agreed.</summary>
    private IReadOnlyList<JudgeAgreement> Judges => AgreementView.Of(Agreement);

    /// <summary>Fetches a judge's labels as a zip and hands it to the browser to save.</summary>
    private async Task DownloadAsync(string grader)
    {
        _error = null;
        try
        {
            byte[] zip = await Api.GetBytesAsync(
                $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/labels?grader={Uri.EscapeDataString(grader)}");
            await Js.InvokeVoidAsync("louisDownload", $"{grader}-labels-{RunId}.zip", Convert.ToBase64String(zip),
                "application/zip");
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }
}
