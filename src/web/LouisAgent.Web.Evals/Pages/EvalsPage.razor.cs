using System.Net;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Web.Api;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Pages;

/// <summary>
/// The evals' page: the host's evals read once when it opens, and again on Reload, each drawn as its card. A
/// refused key shows the frame's key box, as on every admin page.
/// </summary>
/// <remarks>
/// It shows what it last read and nothing polls (ADR 0042). The guide is linked through the site's own docs address
/// when the site names one, and named in words when it doesn't, so nothing is wired by hand (ADR 0120; Louis's pick,
/// 2026-11-23, over a fixed GitHub link and over no link).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public partial class EvalsPage
{
    /// <summary>The guide on running an eval, by its path in the docs.</summary>
    private const string GuidePath = "src/docs/guides/run-an-eval.md";

    private EvalsReply? _evals;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private EvalsApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The guide's address in the site's docs, or none when the site names no docs.</summary>
    private string? GuideLink => Services.GetService<DocsAddress>()?.For(GuidePath);

    /// <summary>Reads the admin's key, then the evals.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads the host's evals; a refused key asks for the key, any other failure says what it was.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _evals = await Api.ListAsync();
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _loading = false;
    }
}
