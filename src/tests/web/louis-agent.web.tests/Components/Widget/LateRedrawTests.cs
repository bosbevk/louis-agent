using System.Reflection;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// FX150: CI's test host crashed on a redraw the stream's throttle fired after a test had let the page's services go;
/// the widget looked the business windows up in those services on every redraw, and the exception escaped on a timer's
/// thread. The widget takes them once, when it starts, so a late redraw reaches for nothing that is gone.
/// </summary>
public class LateRedrawTests
{
    /// <summary>A redraw that comes after the page's services are gone throws nothing.</summary>
    [Test]
    public void ALateRedraw_AfterThePageIsGone_ThrowsNothing()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        var widget = ctx.Render<ChatWidget>(parameters => parameters.Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Ask the guide").Add(w => w.Greeting, "Hi.").Add(w => w.Placeholder, "Ask…")).Instance;
        var redraw = typeof(ChatWidget).GetMethod("OnRunnerChanged", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // The page goes, as a test's end or a navigation lets it go, before the stream's last catch-up fires.
        ctx.Services.Dispose();

        Assert.DoesNotThrow(() => redraw.Invoke(widget, null));
    }
}
