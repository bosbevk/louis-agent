using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Screens.Acting;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Screens.Acting;

/// <summary>
/// A screen's action as a button. One that destroys something asks first and takes a second click, never a browser
/// dialog, and whatever comes of it goes back to the table.
/// </summary>
public class ScreenActionButtonTests
{
    private static readonly ScreenActionReply Remove = new("remove", "Remove", "row", "Remove this recipe?");
    private static readonly ScreenActionReply Refresh = new("refresh", "Refresh", "screen", null);

    /// <summary>The fake API: every address it was asked, and what it answers.</summary>
    private sealed class FakeActions
    {
        public List<string> Asked { get; } = [];
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        /// <summary>Answers one action, keeping its address and body.</summary>
        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Asked.Add($"{request.RequestUri!.AbsolutePath} {body}");
            if (Status != HttpStatusCode.OK)
                return new HttpResponseMessage(Status) { Content = new StringContent("no") };

            var answer = new ScreenAnswerReply(true, "Removed.", new Dictionary<string, string>());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(answer, options: AdminJson.SnakeCase)
            };
        }
    }

    /// <summary>The button over that API, and where its answers land.</summary>
    private static (IRenderedComponent<ScreenActionButton> Button, List<ScreenAnswerReply> Answers) Render(BunitContext ctx,
        FakeActions api, ScreenActionReply action, string? rowId = "row-1")
    {
        var answers = new List<ScreenAnswerReply>();
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(api.Answer)) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));

        var button = ctx.Render<ScreenActionButton>(p => p
            .Add(c => c.ScreenName, "dishes")
            .Add(c => c.Action, action)
            .Add(c => c.RowId, rowId)
            .Add(c => c.OnAnswer, EventCallback.Factory.Create<ScreenAnswerReply>(new object(), answers.Add)));
        return (button, answers);
    }

    /// <summary>One that asks first does nothing on the first click but ask.</summary>
    [Test]
    public void TheFirstClick_OnlyAsks()
    {
        using var ctx = new BunitContext();
        var api = new FakeActions();
        var (button, answers) = Render(ctx, api, Remove);

        button.Find(".screen-action").Click();

        Assert.That(api.Asked, Is.Empty);
        Assert.That(answers, Is.Empty);
        Assert.That(button.Find(".screen-confirm").TextContent, Does.Contain("Remove this recipe?"));
    }

    /// <summary>The second click does it, for this row, and the answer goes back.</summary>
    [Test]
    public void TheSecondClick_DoesIt()
    {
        using var ctx = new BunitContext();
        var api = new FakeActions();
        var (button, answers) = Render(ctx, api, Remove);
        button.Find(".screen-action").Click();

        button.Find(".screen-confirm-yes").Click();

        button.WaitForAssertion(() => Assert.That(answers, Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));
        Assert.That(api.Asked.Single(), Does.StartWith("/v1/admin/screens/dishes/actions/remove").And.Contain("row-1"));
        Assert.That(answers[0].Message, Is.EqualTo("Removed."));
    }

    /// <summary>Saying no leaves everything as it was, and the button back.</summary>
    [Test]
    public void SayingNo_DoesNothing()
    {
        using var ctx = new BunitContext();
        var api = new FakeActions();
        var (button, _) = Render(ctx, api, Remove);
        button.Find(".screen-action").Click();

        button.Find(".screen-confirm-no").Click();

        Assert.That(api.Asked, Is.Empty);
        Assert.That(button.FindAll(".screen-confirm"), Is.Empty);
        Assert.That(button.Find(".screen-action").TextContent, Is.EqualTo("Remove"));
    }

    /// <summary>One with nothing to ask does it on the first click, with no row.</summary>
    [Test]
    public void WithNoQuestion_OneClickDoesIt()
    {
        using var ctx = new BunitContext();
        var api = new FakeActions();
        var (button, answers) = Render(ctx, api, Refresh, rowId: null);

        button.Find(".screen-action").Click();

        button.WaitForAssertion(() => Assert.That(answers, Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));
        Assert.That(api.Asked.Single(), Does.StartWith("/v1/admin/screens/dishes/actions/refresh"));
    }

    /// <summary>When the API says no, the answer says why rather than the page breaking.</summary>
    [Test]
    public void AFailure_ComesBackWithItsReason()
    {
        using var ctx = new BunitContext();
        var (button, answers) = Render(ctx, new FakeActions { Status = HttpStatusCode.Unauthorized }, Refresh, rowId: null);

        button.Find(".screen-action").Click();

        button.WaitForAssertion(() => Assert.That(answers, Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));
        Assert.That(answers[0].Done, Is.False);
        Assert.That(answers[0].Message, Is.Not.Empty);
    }
}
