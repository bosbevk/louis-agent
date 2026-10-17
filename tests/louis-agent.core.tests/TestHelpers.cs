using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Usage;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests;

internal static class TestPaths
{
    public static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    /// <summary>The built-in prompts in core (CL8 step 4), embedded and read only from the assembly.</summary>
    public static string Prompts { get; } = Path.Combine(RepoRoot, "louis-agent.core", "Prompts");

    public static AgentOptions Agent(string? paymoKey = null) => new()
    {
        WorkspaceRoot = RepoRoot,
        PaymoApiKey = paymoKey,
        // No network in tests: AgentHost.Build would otherwise fetch exchange rates from Frankfurter (F14).
        RatesUrl = null,
    };

    /// <summary>
    /// The tool objects of the named toolsets, built by the catalogue as a session's are (F5: an engine has only the
    /// toolsets it is given). <paramref name="webSearch"/> replaces the configured search provider.
    /// </summary>
    public static IReadOnlyList<object> Toolsets(AgentOptions options, IEnumerable<string> names, IWebSearchProvider? webSearch = null)
    {
        var context = new ToolsetContext(options, webSearch ?? WebSearchProviderFactory.Create(options));
        return ToolsetCatalog.BuildTools(ToolsetCatalog.Resolve(names, context), context);
    }
}

/// <summary>Scripted IChatClient: each call is answered by the next delegate and the requests are recorded.</summary>
internal sealed class FakeChatClient(params Func<IList<ChatMessage>, ChatResponse>[] script) : IChatClient
{
    private int _call;
    public List<(List<ChatMessage> Messages, ChatOptions? Options)> Requests { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var snapshot = messages.ToList();
        Requests.Add((snapshot, options));
        return Task.FromResult(script[_call++](snapshot));
    }

    /// <summary>Streams the scripted response as one update per content item, ending with its finish reason (default Stop, as real providers send).</summary>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var message in response.Messages)
            foreach (var content in message.Contents)
                yield return new ChatResponseUpdate(message.Role, [content]) { MessageId = message.MessageId };
        yield return new ChatResponseUpdate { FinishReason = response.FinishReason ?? ChatFinishReason.Stop };
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpMethod Method, string Url, string? Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.Method, request.RequestUri!.ToString(), body));
        return respond(request);
    }
}

/// <summary>Streams one text update, then throws the given exception, as a provider does when a stream breaks.</summary>
internal sealed class FailingStreamClient(Exception exception) : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw exception;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "partial");
        await Task.Yield();
        throw exception;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

/// <summary>Collects records in memory for assertions.</summary>
internal sealed class ListUsageSink : IUsageSink
{
    public List<UsageRecord> Records { get; } = [];

    public UsageRecord Record(UsageRecord record)
    {
        Records.Add(record);
        return record;
    }
}
