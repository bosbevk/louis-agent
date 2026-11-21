using System.Net;
using Bunit.TestDoubles;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin.Reset;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Admin.Reset;

/// <summary>
/// F153: Reset my session puts this browser back to a first visit. Every session it saved, in every list of chats, is
/// ended on the API, a session the server answers no for stopping none of the rest; then both storages are cleared
/// but for the admin key, and the page reloads (Louis's pick: everything a first visit doesn't have).
/// </summary>
public class SessionResetTests
{
    /// <summary>The guide's list in the bar and the chat page's, each with the sessions they saved.</summary>
    private static readonly Dictionary<string, string[]> Saved = new()
    {
        ["louis-agent.widget-chats"] = ["guide-1", "guide-2"],
        ["louis-agent.chats"] = ["page-1"]
    };

    /// <summary>A saved list of chats, as the chat writes it.</summary>
    private static string ListOf(string[] sessions) =>
        "[" + string.Join(",", sessions.Select(id => $"{{\"sessionId\":\"{id}\",\"title\":\"t\",\"turns\":[]}}")) + "]";

    /// <summary>
    /// Every saved session gets one DELETE, the one the server no longer has does not stop the others, and then the
    /// storages are cleared keeping the admin key, and the page reloads, in that order.
    /// </summary>
    [Test]
    public async Task EverySavedSession_IsEnded_ThenTheBrowserForgets_AndReloads()
    {
        using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Reset);
        module.Setup<string[]>("chatKeys").SetResult(Saved.Keys.ToArray());
        var forget = module.SetupVoid("forget", ChatStore.AdminKeyKey);
        forget.SetVoidResult();
        foreach (var (key, sessions) in Saved)
            ctx.JSInterop.Setup<string?>("localStorage.getItem", key).SetResult(ListOf(sessions));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // The server no longer has the first guide session; the others it ends.
        var handler = new StubHandler(request => new HttpResponseMessage(
            request.RequestUri!.AbsolutePath.EndsWith("guide-1") ? HttpStatusCode.NotFound : HttpStatusCode.NoContent));
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        var reset = new SessionReset(ctx.JSInterop.JSRuntime, settings, api, navigation);

        await reset.RunAsync();

        var history = ((BunitNavigationManager)navigation).History;
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests.Select(r => $"{r.Method} {r.Url}"), Is.EquivalentTo(new[]
            {
                "DELETE http://localhost/v1/sessions/guide-1", "DELETE http://localhost/v1/sessions/guide-2",
                "DELETE http://localhost/v1/sessions/page-1"
            }));
            Assert.That(forget.Invocations, Has.Count.EqualTo(1));
            Assert.That(history, Has.Count.EqualTo(1));
            Assert.That(history.Single().Options.ForceLoad, Is.True);
        });
    }
}
