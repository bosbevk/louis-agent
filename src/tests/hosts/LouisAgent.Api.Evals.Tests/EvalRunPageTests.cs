using LouisAgent.Evals.Contracts.Runs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// F126.3.3: a run started through the route, on a host with a site, sends its agent the page its conversation names,
/// as the site's widget would, so the agent measured gets what production's agent gets.
/// </summary>
public class EvalRunPageTests
{
    /// <summary>The page the test's conversation names.</summary>
    [Route("/greeting")]
    public sealed class GreetingPage : ComponentBase
    {
        /// <summary>Its main part, with words the agent's model can be seen to get.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, "<main><h1>Welcome to the test site</h1><p>We greet everyone.</p></main>");
    }

    /// <summary>A run of a conversation on a page sends the page with its ask, and says it did.</summary>
    [Test]
    public async Task ARunOnAPage_SendsThePage_AndSaysItDid()
    {
        var model = EvalsTestHost.Answering("Hello there.");
        await using var host = await EvalsTestHost.StartAsync(model, page: "/greeting");

        var run = await host.RunToTheEndAsync();

        Assert.That(run.Status, Is.EqualTo(RunStatus.Finished), run.StoppedBecause);
        Assert.That(run.PageSent, Is.True);
        Assert.That(model.Requests.SelectMany(request => request.Messages).Select(message => message.Text),
            Has.Some.Contains("Welcome to the test site"));
    }
}
