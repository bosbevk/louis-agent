using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Studio;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Studio;

/// <summary>
/// Keeps a graded conversation as a transcript draft: a pass as a reference, a fail as a known-bad with the reason the
/// person types. It shows the file the transcript will be, or why the API wouldn't keep it.
/// </summary>
/// <remarks>
/// Parameters down: the review panel says which conversation and what this person said; which button shows is
/// <see cref="KeepOffers"/>'s rule, a plain class with its own tests (ADR 0034).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
public partial class KeepButtons : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    /// <summary>The conversation that was graded.</summary>
    [Parameter, EditorRequired]
    public ConversationRef Conversation { get; set; } = null!;

    /// <summary>What this person said of it: pass, fail or unsure.</summary>
    [Parameter]
    public string? Verdict { get; set; }

    private string _reason = "";
    private string? _done;
    private string? _error;
    private bool _busy;

    /// <summary>What this verdict offers to keep.</summary>
    private KeepOffer Offer => KeepOffers.For(Verdict);

    /// <summary>Loads the reviewer's name, which the draft is kept under.</summary>
    protected override async Task OnInitializedAsync()
    {
        Reviewer.Changed += Redraw;
        await Reviewer.LoadAsync();
    }

    /// <summary>Redraws when the reviewer's name changes elsewhere on the page.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    /// <summary>Keeps a passed conversation as a reference.</summary>
    private Task KeepReferenceAsync() => KeepAsync("reference", null);

    /// <summary>Keeps a failed conversation as a known-bad, with its reason.</summary>
    private Task KeepBadAsync() => KeepAsync("bad", _reason.Trim());

    /// <summary>Posts the transcript draft and says which file it will be, or why not.</summary>
    private async Task KeepAsync(string kind, string? reason)
    {
        _done = null;
        _error = null;
        if (Reviewer.Name is not { } name)
        {
            _error = StudioText.NeedsAName;
            return;
        }

        _busy = true;
        try
        {
            // Post it under the run; the API answers with the draft and its file name.
            var key = new ConversationKeyRow(Conversation.Mode, Conversation.Repeat, Conversation.Task);
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(Conversation.Run)}/transcripts")
            {
                Content = JsonContent.Create(new TranscriptDraftRequest(key, kind, reason, name),
                    options: AdminJson.SnakeCase)
            };
            using var response = await Api.SendAsync(request);
            var draft = await response.Content.ReadFromJsonAsync<TranscriptDraftRow>(AdminJson.SnakeCase);
            _done = StudioText.TranscriptKept(draft!.FileName);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Stops listening for the reviewer's name.</summary>
    public void Dispose() => Reviewer.Changed -= Redraw;
}
