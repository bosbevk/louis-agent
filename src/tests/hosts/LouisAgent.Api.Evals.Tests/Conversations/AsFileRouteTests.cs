using System.Net;
using static LouisAgent.Api.Evals.Tests.EvalsTestHost;

namespace LouisAgent.Api.Evals.Tests.Conversations;

/// <summary>
/// F126.4: a run's conversation is answered as a block for the eval file, to copy and change: its asks, facts and
/// criteria, with the page the eval file still gives it.
/// </summary>
public class AsFileRouteTests
{
    /// <summary>The block holds the conversation's ask and its page from the eval file, as the file has it.</summary>
    [Test]
    public async Task AConversation_IsAnsweredAsABlockForTheFile_WithItsPage()
    {
        await using var host = await StartAsync(Answering("Hello."), page: "/greeting");
        var run = await host.RunToTheEndAsync();

        using var answer = await host.Client.GetAsync($"/v1/admin/evals/runs/{run.Id}/hello/as-file");
        string block = await answer.Content.ReadAsStringAsync();

        Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.OK), block);
        Assert.That(block, Does.Contain("\"id\": \"hello\"").And.Contain("\"page\": \"/greeting\"")
            .And.Contain("\"ask\": \"Hello?\"").And.Contain("\"no-error\""));
    }

    /// <summary>A run or a conversation the host doesn't have is a 404.</summary>
    [Test]
    public async Task AnUnknownRunOrConversation_IsNotFound()
    {
        await using var host = await StartAsync(Answering("Hello."));
        var run = await host.RunToTheEndAsync();

        using var noRun = await host.Client.GetAsync("/v1/admin/evals/runs/20261123-043012-nobody/hello/as-file");
        using var noConversation = await host.Client.GetAsync($"/v1/admin/evals/runs/{run.Id}/nobody/as-file");

        Assert.That((noRun.StatusCode, noConversation.StatusCode),
            Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }
}
