using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens.Acting;

/// <summary>
/// One of a screen's actions as a button. One that destroys something asks first and takes a second click, on the page
/// itself rather than in a browser dialog, and whatever comes of it goes back to the table.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class ScreenActionButton
{
    private bool _asking, _busy;

    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The screen it belongs to.</summary>
    [Parameter, EditorRequired] public string ScreenName { get; set; } = "";

    /// <summary>What it does.</summary>
    [Parameter, EditorRequired] public ScreenActionReply Action { get; set; } = null!;

    /// <summary>The row it acts on; none for a screen's action.</summary>
    [Parameter] public string? RowId { get; set; }

    /// <summary>What came of it.</summary>
    [Parameter] public EventCallback<ScreenAnswerReply> OnAnswer { get; set; }

    /// <summary>The first click: asks, if there's a question, or does it.</summary>
    private async Task ClickAsync()
    {
        if (Action.Confirm is not null)
        {
            _asking = true;
            return;
        }

        await DoAsync();
    }

    /// <summary>Puts the button back, having done nothing.</summary>
    private void SayNo() => _asking = false;

    /// <summary>Does it. A failure comes back as an answer with its reason, and the table shows it the same.</summary>
    private async Task DoAsync()
    {
        _asking = false;
        _busy = true;
        ScreenAnswerReply answer;
        try
        {
            string address = $"v1/admin/screens/{Uri.EscapeDataString(ScreenName)}/actions/{Uri.EscapeDataString(Action.Name)}";
            answer = await Api.PostAsync<ScreenAnswerReply>(address, new ScreenActionRequest(RowId), AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            answer = new ScreenAnswerReply(false, ex.Message, new Dictionary<string, string>());
        }
        catch (HttpRequestException ex)
        {
            answer = new ScreenAnswerReply(false, ex.Message, new Dictionary<string, string>());
        }

        _busy = false;
        await OnAnswer.InvokeAsync(answer);
    }
}
