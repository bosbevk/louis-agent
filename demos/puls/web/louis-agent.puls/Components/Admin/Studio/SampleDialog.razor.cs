using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Studio;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Studio;

/// <summary>
/// Makes a sample from a conversation of a stored run: reads the scenario it ran, lets the person change the id, the
/// expected values, the categories and an ideal answer, and keeps it as a draft of the eval under their name.
/// </summary>
/// <remarks>
/// Parameters down, callbacks up, as the chats dialog: the card says which conversation and hears when to close. The
/// form's rules live in <see cref="SampleForm"/>, a plain class with its own tests (ADR 0034).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
public partial class SampleDialog : IDisposable
{
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;

    /// <summary>The conversation the sample comes from.</summary>
    [Parameter, EditorRequired]
    public ConversationRef Conversation { get; set; } = null!;

    /// <summary>The person closed the dialog.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    private ScenarioRow? _scenario;
    private SampleForm _form = new();
    private string? _done;
    private string? _error;
    private bool _busy;

    /// <summary>The run's address, escaped, as the routes under it start.</summary>
    private string RunPath => $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(Conversation.Run)}";

    /// <summary>Loads the reviewer's name and the scenario the form starts from.</summary>
    protected override async Task OnInitializedAsync()
    {
        Reviewer.Changed += Redraw;
        await Reviewer.LoadAsync();
        try
        {
            _scenario = await Api.GetAsync<ScenarioRow>(
                $"{RunPath}/scenarios/{Uri.EscapeDataString(Conversation.Task)}", AdminJson.SnakeCase);
            _form = SampleForm.From(_scenario);
        }
        catch (AgentApiException ex)
        {
            // No scenario (an eval with no folder, a task it doesn't have): the API says which.
            _error = ex.Message;
        }
    }

    /// <summary>Redraws when the reviewer's name changes elsewhere on the page.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    /// <summary>Sends the sample and says what was kept, or why not.</summary>
    private async Task MakeAsync()
    {
        _done = null;
        _error = null;
        if (Reviewer.Name is not { } name)
        {
            _error = StudioText.NeedsAName;
            return;
        }

        var (body, refusal) = _form.Request(Conversation, name);
        if (body is null)
        {
            _error = refusal;
            return;
        }

        _busy = true;
        try
        {
            // Post it under the run; the API answers with the draft it kept.
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{RunPath}/samples")
            {
                Content = JsonContent.Create(body, options: AdminJson.SnakeCase)
            };
            using var response = await Api.SendAsync(request);
            var draft = await response.Content.ReadFromJsonAsync<SampleDraftRow>(AdminJson.SnakeCase);
            _done = StudioText.SampleKept(draft!.SampleId, draft.Eval);
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
