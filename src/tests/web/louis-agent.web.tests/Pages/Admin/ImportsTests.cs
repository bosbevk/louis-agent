using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Imports;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin;

/// <summary>
/// The Uploads page, the same on every site: a card for each upload the packs take, or a component of a pack's own where
/// one is registered, and it knows no pack.
/// </summary>
public class ImportsTests
{
    private static readonly ImportsReply TwoUploads = new(
    [
        new ImportRow("prices", "Average prices.", [new ImportSlotRow("cpi", "The CPI file")]),
        new ImportRow("rates", "Exchange rates.", [new ImportSlotRow("rates", "The rates file")])
    ]);

    /// <summary>A pack's own view of an upload: it says which one it was handed.</summary>
    private sealed class OwnView : ComponentBase, IImportView
    {
        [Parameter] public ImportForm Form { get; set; } = null!;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, $"<p class=\"own-view\">{Form.Upload.Name}</p>");
    }

    /// <summary>The page over an API answering with these.</summary>
    private static IRenderedComponent<Imports> Render(BunitContext ctx, ImportsReply reply, PackViews? views = null,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(views ?? new PackViews());
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = status == HttpStatusCode.OK ? JsonContent.Create(reply, options: AdminJson.SnakeCase) : JsonContent.Create(new { })
        }))
        {
            BaseAddress = new Uri("http://localhost/")
        }, settings));

        var page = ctx.Render<Imports>();
        page.WaitForState(() => page.FindAll(".imports-loading").Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }

    /// <summary>A card for each upload, by default.</summary>
    [Test]
    public void EachUpload_GetsACard()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, TwoUploads);

        Assert.That(page.FindComponents<ImportCard>(), Has.Count.EqualTo(2));
    }

    /// <summary>A registered view draws that upload, and the rest stay cards.</summary>
    [Test]
    public void ARegisteredView_DrawsThatUpload()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, TwoUploads, new PackViews().UseImport<OwnView>("prices"));

        Assert.That(page.Find(".own-view").TextContent, Is.EqualTo("prices"));
        Assert.That(page.FindComponents<ImportCard>(), Has.Count.EqualTo(1));
    }

    /// <summary>With none it says so.</summary>
    [Test]
    public void NoUploads_SaysSo()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, new ImportsReply([]));

        Assert.That(page.Markup, Does.Contain("No pack on this instance takes an upload"));
    }

    /// <summary>A refused key shows the box to enter one.</summary>
    [Test]
    public void ARefusedKey_ShowsTheGate()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, TwoUploads, status: HttpStatusCode.Unauthorized);

        Assert.That(page.FindComponents<AdminGate>(), Has.Count.EqualTo(1));
        Assert.That(page.FindAll(".admin-key"), Has.Count.EqualTo(1));
    }
}
