using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Chat.Composing;
using LouisAgent.Web.Components.Chat.Turns;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Chat.Turns;

/// <summary>
/// What a person acts on around a turn: the message box, and the buttons under a message and an answer. Their words are
/// English unless a site gives its own.
/// </summary>
public class ActionsTests
{
    /// <summary>Under the message: copy and edit, in English.</summary>
    [Test]
    public void UserActions_AreInEnglish()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<UserActions>(parameters => parameters.Add(actions => actions.Turn, new Turn()));

        Assert.That(cut.FindAll(".action").Select(action => action.GetAttribute("title")),
            Is.EqualTo(new[] { "Copy", "Edit the message" }));
    }

    /// <summary>Under the answer: its buttons, in English, and no sources when it has none.</summary>
    [Test]
    public void AnswerActions_AreInEnglish_WithNoSourcesWhenItHasNone()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<AnswerActions>(parameters => parameters.Add(actions => actions.Turn, new Turn()));

        Assert.That(cut.FindAll(".action").Select(action => action.GetAttribute("title")),
            Is.EqualTo(new[] { "Copy the answer", "Try again", "Good answer", "Bad answer", "Share" }));
        Assert.That(cut.FindAll(".action-sources"), Is.Empty);
    }

    /// <summary>The message box asks in English, and its toolbar names its tools.</summary>
    [Test]
    public void TheMessageBox_IsInEnglish()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(new StubHandler(FixtureApi.Answer)) { BaseAddress = new Uri("http://localhost/") },
            settings);
        var text = new ChatText();
        ctx.Services.AddSingleton(text);
        var store = new ChatStore(ctx.JSInterop.JSRuntime, settings);
        var runner = new ChatRunner(api, store, text, () => Task.FromResult("session"));

        var cut = ctx.Render<MessageBox>(parameters => parameters.Add(box => box.Runner, runner));

        Assert.That(cut.Find("textarea").GetAttribute("placeholder"), Is.EqualTo("Ask anything…"));
        Assert.That(cut.Markup, Does.Contain("Upload a file").And.Contain("Search the web").And.Contain("The server's model"));
    }
}
