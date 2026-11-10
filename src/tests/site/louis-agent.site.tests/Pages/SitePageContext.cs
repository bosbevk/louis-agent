using System.Net;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// A context any page of the site renders in: the scripts answered with nothing, the chat's words, and an
/// <see cref="HttpClient"/> on the site's address that serves nothing. The coding agent's page plays a recording
/// (F120), so a test that walks every page needs what that page needs; a recording it can't fetch goes straight to its
/// ending.
/// </summary>
internal static class SitePageContext
{
    /// <summary>A new context, ready for any of the site's pages.</summary>
    public static BunitContext Create()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var nothing = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        ctx.Services.AddSingleton(new HttpClient(nothing) { BaseAddress = new Uri("http://site.test/") });
        // The site's demos, as Program.cs registers them, for the showroom's start (F127.1).
        ctx.Services.AddSingleton(SiteDemos.All);
        return ctx;
    }
}
