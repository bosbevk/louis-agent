using LouisAgent.Web.Api;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Admin.Reset;

/// <summary>
/// Puts this browser back to a first visit (F153): every chat it saved is ended on the API, everything it keeps is
/// forgotten but the admin's key, and the page reloads, so the site opens the way a new visitor sees it.
/// </summary>
/// <remarks>
/// A Facade with a Transaction Script inside it, Louis's pick A of three: one call in front of the module, the chat
/// store, the API and the navigation, and inside it one procedure in order. It was chosen over the steps living in the
/// button's own code, which would mix drawing with work no test could reach alone, and over a script that calls the
/// API itself, which would end sessions without the keys and the address the app already sends. The sessions are
/// ended before the lists that name them are forgotten, so a reset that fails half way still has their ids.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public sealed class SessionReset(IJSRuntime js, ChatSettings settings, AgentApi api, NavigationManager navigation)
{
    /// <summary>Ends every saved session, forgets all this browser keeps but the admin's key, and reloads.</summary>
    public async Task RunAsync()
    {
        await using var module = new JsModule(js, FrameworkScripts.Reset);

        // Every list of chats this browser saved, read as the chat reads it, each session ended in turn.
        string[] lists = await module.InvokeAsync<string[]>("chatKeys");
        foreach (string list in lists)
        {
            var store = new ChatStore(js, settings, list);
            await store.LoadAsync();
            foreach (var chat in store.Chats)
                await EndAsync(chat.SessionId);
        }

        await module.InvokeVoidAsync("forget", ChatStore.AdminKeyKey);
        navigation.Refresh(forceReload: true);
    }

    /// <summary>
    /// Ends one session. One the server refuses or can't be reached for is left to expire on its own, so it stops
    /// none of the rest.
    /// </summary>
    private async Task EndAsync(string sessionId)
    {
        try
        {
            await api.DeleteSessionAsync(sessionId);
        }
        catch (AgentApiException)
        {
            // The server said no; the browser forgets the session all the same.
        }
        catch (HttpRequestException)
        {
            // The server can't be reached; the browser forgets the session all the same.
        }
    }
}
