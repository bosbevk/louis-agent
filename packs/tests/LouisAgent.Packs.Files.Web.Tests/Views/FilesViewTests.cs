using System.Net;
using System.Net.Http.Json;
using LouisAgent.Packs.Files.Web.Reading;
using LouisAgent.Packs.Files.Web.Views;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Packs.Files.Web.Tests.Views;

/// <summary>
/// The workspace's files as an admin screen: the tree beside the open file. Its buttons hand the chat a draft and never
/// send anything. This project sees both sides, so it also holds the screen's name to the one the pack gave it.
/// </summary>
public class FilesViewTests
{
    private const string Path = "src/app.cs";

    /// <summary>The API's answers: one file in the folder, and that file; or a refusal when reading it.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request, bool fileFails)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("/entries"))
            return Json(new WorkspaceFolder("", [new WorkspaceEntry("app.cs", Path, "file", 5, false)]));

        return fileFails
            ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = JsonContent.Create(new { }) }
            : Json(new WorkspaceFile(Path, "app.cs", 5, "hello"));
    }

    /// <summary>A reply as the workspace routes write it.</summary>
    private static HttpResponseMessage Json<T>(T reply) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(reply) };

    /// <summary>The view with its tree loaded, and the handoff its buttons use.</summary>
    private static (IRenderedComponent<FilesView> View, ChatHandoff Handoff) Render(BunitContext ctx, bool fileFails = false)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handoff = new ChatHandoff("chat");
        ctx.Services.AddSingleton(handoff);
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(request => Answer(request, fileFails)))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));

        var view = ctx.Render<FilesView>();
        view.WaitForAssertion(() => Assert.That(view.FindAll(".tree-row.file"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        return (view, handoff);
    }

    /// <summary>The view with its one file open.</summary>
    private static (IRenderedComponent<FilesView> View, ChatHandoff Handoff) RenderOpen(BunitContext ctx)
    {
        var (view, handoff) = Render(ctx);
        view.Find(".tree-row.file").Click();
        view.WaitForAssertion(() => Assert.That(view.FindAll(".file-viewer"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        return (view, handoff);
    }

    /// <summary>Nothing is open at first; clicking a file shows it, and marks it in the tree.</summary>
    [Test]
    public void ClickingAFile_ShowsIt()
    {
        using var ctx = new BunitContext();
        var (view, _) = Render(ctx);
        Assert.That(view.FindAll(".file-viewer"), Is.Empty);

        view.Find(".tree-row.file").Click();

        view.WaitForAssertion(() => Assert.That(view.Find(".file-path").TextContent, Is.EqualTo(Path)),
            TimeSpan.FromSeconds(10));
        Assert.That(view.Find(".tree-row.file").ClassList, Does.Contain("selected"));
    }

    /// <summary>A file that can't be read says why, and the tree stays.</summary>
    [Test]
    public void AFileThatCannotBeRead_SaysWhy()
    {
        using var ctx = new BunitContext();
        var (view, _) = Render(ctx, fileFails: true);

        view.Find(".tree-row.file").Click();

        view.WaitForAssertion(() => Assert.That(view.FindAll(".pack-detail .tree-error"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(view.FindAll(".tree-row.file"), Has.Count.EqualTo(1));
    }

    /// <summary>Review opens the chat with the prompt as a draft.</summary>
    [Test]
    public void Review_OpensTheChat_WithAPrompt()
    {
        using var ctx = new BunitContext();
        var (view, handoff) = RenderOpen(ctx);

        view.Find(".file-actions .send").Click();

        Assert.That(handoff.Take(), Does.StartWith($"Review `{Path}` from the workspace."));
        Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/chat"));
    }

    /// <summary>Ask opens the chat with the file's name ready, for the person to finish.</summary>
    [Test]
    public void Ask_OpensTheChat_WithTheFilesName()
    {
        using var ctx = new BunitContext();
        var (view, handoff) = RenderOpen(ctx);

        view.Find(".file-actions .secondary").Click();

        Assert.That(handoff.Take(), Is.EqualTo($"About `{Path}`: "));
    }

    /// <summary>Closing the file leaves the tree.</summary>
    [Test]
    public void Closing_HidesTheFile()
    {
        using var ctx = new BunitContext();
        var (view, _) = RenderOpen(ctx);

        view.Find(".file-actions .icon").Click();

        Assert.That(view.FindAll(".file-viewer"), Is.Empty);
    }

    /// <summary>The view registers under the name the pack gave its screen, so the site can draw it.</summary>
    [Test]
    public void TheView_RegistersUnderThePacksName()
    {
        var views = new PackViews();

        FilesWebViews.AddTo(views);

        var name = new FilesPack().Screens.Single().Spec.Name;
        Assert.That(views.ScreenView(name), Is.EqualTo(typeof(FilesView)));
    }
}
