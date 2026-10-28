using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin;

/// <summary>
/// A category's line links to its tasks (`?task=`), and the explorer opens that task's conversation at once.
/// </summary>
public class RunExplorerOpenTaskTests
{
    /// <summary>One mode's JSON for a repeat.</summary>
    private static string ModeJson(int repeat) =>
        $$"""{"name":"puls-cook","repeat":{{repeat}},"toolsets":["{{SampleToolset.Alpha}}"]}""";

    /// <summary>One conversation's JSON for a repeat and a task.</summary>
    private static string ConversationJson(int repeat, string task) =>
        $$"""{"mode":"puls-cook","repeat":{{repeat}},"task":"{{task}}","role":null,"errors":0}""";

    /// <summary>A run with this many repeats, each with two conversations.</summary>
    private static string Run(int repeats) => SampleScenario.Fill($$"""
                                                                    {"run":{"id":"run123456789","check":"puls-cook","repeat":{{repeats}}},"modes":[{{string.Join(",", Enumerable.Range(1, repeats).Select(ModeJson))}}],
                                                                     "conversations":[{{string.Join(",", Enumerable.Range(1, repeats).SelectMany(r => new[] { "<ScenarioAlpha>", "<ScenarioBeta>" }.Select(t => ConversationJson(r, t))))}}],
                                                                     "turns":[],"requests":[]}
                                                                    """);

    /// <summary>Renders the explorer on a run, opened at a task if one is given.</summary>
    private static IRenderedComponent<RunExplorer> Render(string? openTask, int repeats = 1)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // What the explorer needs: an API that answers nothing, a reviewer, and the notes state.
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new RunNotesState());

        // The run as the page reads it from the API: the contract's stored run reply.
        var run = JsonSerializer.Deserialize<StoredRunReply>(Run(repeats), AdminJson.SnakeCase)!;
        return ctx.Render<RunExplorer>(p => p.Add(x => x.Run, run).Add(x => x.OpenTask, openTask));
    }

    /// <summary>A task to open takes you straight to its conversation.</summary>
    [Test]
    public void ATaskToOpen_OpensItsConversation_AtOnce() =>
        Assert.That(Render(SampleScenario.Beta).Find(".crumb.here").TextContent, Is.EqualTo(SampleScenario.Beta));

    /// <summary>
    /// Changing the repeat keeps the conversation you are in when the other repeat has it too, and falls back to the
    /// overview only when it doesn't.
    /// </summary>
    [Test]
    public void ChangingTheRepeat_KeepsTheOpenConversation()
    {
        var cut = Render(SampleScenario.Beta, repeats: 2);

        cut.FindAll(".segmented button").First(b => b.TextContent == "2").Click();

        Assert.That(cut.Find(".crumb.here").TextContent, Is.EqualTo(SampleScenario.Beta));
        Assert.That(cut.Find(".segmented button.active").TextContent, Is.EqualTo("2"));
    }

    /// <summary>With no task to open, the explorer stays at the run.</summary>
    [Test]
    public void NoTaskToOpen_StaysAtTheRun() =>
        Assert.That(Render(null).Find(".crumb.here").TextContent, Is.EqualTo("Run"));
}
