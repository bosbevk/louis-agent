using System.Net;
using System.Net.Http.Json;
using LouisAgent.Packs.Git.Web.Branches;
using LouisAgent.Packs.Git.Web.Branches.Records;
using LouisAgent.Packs.Git.Web.Changes;
using LouisAgent.Packs.Git.Web.Views;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Packs.Git.Web.Tests.Views;

/// <summary>
/// Changes and Branches as admin screens: each list beside what's open. A review hands the chat a draft and sends
/// nothing. This project sees both sides, so it also holds the screens' names to the ones the pack gave them.
/// </summary>
public class GitViewsTests
{
    private const string Path = "src/app.cs";

    private static readonly GitBranch Feature =
        new("feature/x", "abc1234", "Add x", "Someone", "2026-11-06T10:00:00Z", false, 1, 0, false);

    /// <summary>The API's answers: one changed file with its diff, and one branch to merge.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/git/status") || path.EndsWith("/git/stage"))
            return Json(new GitStatus("main", [new GitFile(Path, null, " ", "M")], null));
        if (path.EndsWith("/git/diff"))
            return Json(new GitDiff(Path, false, false, "@@ -1 +1 @@\n-a\n+b\n", false));
        if (path.EndsWith("/git/branches"))
            return Json(new GitBranches("main", [Feature], null));
        if (path.EndsWith("/git/commits"))
            return Json(new GitLog([]));

        return path.Contains("/git/branches/")
            ? Json(new GitBranchDetail(Feature, "main", [], "", false))
            : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = JsonContent.Create(new { }) };
    }

    /// <summary>A reply as the git routes write it.</summary>
    private static HttpResponseMessage Json<T>(T reply) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(reply) };

    /// <summary>A context with what the views ask for, and the handoff their buttons use.</summary>
    private static ChatHandoff Prepare(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handoff = new ChatHandoff("chat");
        var api = new AgentApi(new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings());
        var git = new GitState(api);
        ctx.Services.AddSingleton(handoff);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(git);
        ctx.Services.AddSingleton(new GitHistory(api, git));
        return handoff;
    }

    /// <summary>Changes, with its one file's diff open.</summary>
    private static IRenderedComponent<GitChangesView> OpenDiff(BunitContext ctx)
    {
        var view = ctx.Render<GitChangesView>();
        view.WaitForAssertion(() => Assert.That(view.FindAll(".change-file"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        view.Find(".change-file").Click();
        view.WaitForAssertion(() => Assert.That(view.FindAll(".file-viewer"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        return view;
    }

    /// <summary>Clicking a changed file shows its diff beside the list, and marks it.</summary>
    [Test]
    public void ClickingAChange_ShowsItsDiff()
    {
        using var ctx = new BunitContext();
        Prepare(ctx);

        var view = OpenDiff(ctx);

        Assert.That(view.Find(".pack-detail .file-path").TextContent, Is.EqualTo(Path));
        Assert.That(view.Find(".change-row").ClassList, Does.Contain("selected"));
    }

    /// <summary>Review opens the chat with the prompt as a draft.</summary>
    [Test]
    public void ReviewingADiff_OpensTheChat_WithAPrompt()
    {
        using var ctx = new BunitContext();
        var handoff = Prepare(ctx);
        var view = OpenDiff(ctx);

        view.Find(".file-actions .send").Click();

        Assert.That(handoff.Take(), Does.StartWith($"Review the uncommitted changes to `{Path}`."));
    }

    /// <summary>Review all opens the chat with a review of everything uncommitted.</summary>
    [Test]
    public void ReviewAll_OpensTheChat_WithAPrompt()
    {
        using var ctx = new BunitContext();
        var handoff = Prepare(ctx);
        var view = ctx.Render<GitChangesView>();
        view.WaitForAssertion(() => Assert.That(view.FindAll(".change-file"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));

        view.FindAll(".selection-bar button").Single(button => button.TextContent == "Review all").Click();

        Assert.That(handoff.Take(), Does.StartWith("Review all my uncommitted changes."));
    }

    /// <summary>Closing the diff leaves the list.</summary>
    [Test]
    public void ClosingADiff_LeavesTheList()
    {
        using var ctx = new BunitContext();
        Prepare(ctx);
        var view = OpenDiff(ctx);

        view.Find(".file-actions .icon").Click();

        Assert.That(view.FindAll(".file-viewer"), Is.Empty);
        Assert.That(view.FindAll(".change-file"), Has.Count.EqualTo(1));
    }

    /// <summary>Clicking a branch shows what it holds beside the list, and marks it.</summary>
    [Test]
    public void ClickingABranch_ShowsIt()
    {
        using var ctx = new BunitContext();
        Prepare(ctx);
        var view = ctx.Render<GitBranchesView>();
        view.WaitForAssertion(() => Assert.That(view.FindAll(".history-item"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));

        view.Find(".history-item").Click();

        view.WaitForAssertion(() => Assert.That(view.Find(".pack-detail .file-path").TextContent, Does.Contain("feature/x")),
            TimeSpan.FromSeconds(10));
        Assert.That(view.Find(".history-row").ClassList, Does.Contain("selected"));
    }

    /// <summary>Both views register under the names the pack gave its screens, so a site can draw them.</summary>
    [Test]
    public void TheViews_RegisterUnderThePacksNames()
    {
        var views = new PackViews();

        GitWebViews.AddTo(views);

        var names = new GitPack().Screens.Select(screen => screen.Spec.Name).ToList();
        Assert.That(names.Select(views.ScreenView), Is.EqualTo(new[] { typeof(GitChangesView), typeof(GitBranchesView) }));
    }
}
