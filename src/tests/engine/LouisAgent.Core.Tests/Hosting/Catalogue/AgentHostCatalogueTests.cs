using LouisAgent.Composition.Packs.Files;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Sessions.Files;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Hosting.Catalogue;

/// <summary>
/// F171.1 step 8: a program whose pack needs a model builds its catalogue from a function the host calls with the
/// models, each already wrapped, so the pack's tool reads a file the chat holds through the model it was given, and
/// the ledger gets the reading as the tool's.
/// </summary>
[NonParallelizable]
public class AgentHostCatalogueTests
{
    /// <summary>What the made-up provider answers: the account, as the record's JSON.</summary>
    private const string Answer = """{"accountNumber":"9876543210","totalDue":3005.00}""";

    private string _logs = null!;

    [SetUp]
    public void SetUp() => _logs = Directory.CreateTempSubdirectory("tool-models-").FullName;

    [TearDown]
    public void TearDown()
    {
        AgentLogging.Configure(null);
        Directory.Delete(_logs, recursive: true);
    }

    /// <summary>A factory that gives every model the one client a test made.</summary>
    private sealed class OneClientFactory(IChatClient client) : ILlmClientFactory
    {
        public IChatClient Create(LlmOptions options) => client;

        public IUsageMapper CreateUsageMapper(LlmOptions options) => new StandardUsageMapper();
    }

    /// <summary>A provider that fails, or honours a cancellation, before it answers.</summary>
    private sealed class FailingClient(Exception failure) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw failure;
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    /// <summary>The chat's one file: a photo of the account.</summary>
    private sealed class OnePhoto : ISessionFiles
    {
        public static readonly SessionFile Photo = new("account.jpg", FileMediaType.Jpeg, new byte[] { 0xFF, 0xD8 });

        public SessionFile? Find(string name) => name == Photo.Name ? Photo : null;

        public void Forget(string name)
        {
        }
    }

    /// <summary>A host whose catalogue is built from the models, with the ledger on in this test's folder.</summary>
    private AgentHostContext Host(IChatClient provider, Action<ToolModels>? seen = null)
    {
        AgentOptions agent = TestPaths.Agent();
        agent.LogDirectory = _logs;
        agent.UsageLedger = true;
        return AgentHost.Build(new LlmOptions(), agent, new OneClientFactory(provider), models =>
        {
            seen?.Invoke(models);
            return new AgentCatalog([], [new ReadSamplePack(models.Instance)]);
        });
    }

    /// <summary>
    /// Runs the pack's tool on the chat's photo, inside a turn, and gives back its text and the turn.
    /// </summary>
    private static async Task<(string Text, UsageScope Turn)> ReadAsync(AgentHostContext host,
        CancellationToken cancellationToken = default)
    {
        var engine = host.Engines.GetOrCreate([ReadSamplePack.PackName]);
        var tool = engine.Tools.OfType<AIFunction>().Single(t => t.Name == nameof(ReadSampleTools.ReadSample));
        var arguments = new AIFunctionArguments { ["name"] = OnePhoto.Photo.Name };

        var turn = UsageScope.Begin(UsagePurpose.Turn, host: "api", session: "s-1", turn: 1);
        using (turn)
        using (SessionFiles.Use(new OnePhoto()))
        {
            object? result = await tool.InvokeAsync(arguments, cancellationToken);
            return (result?.ToString() ?? "", turn);
        }
    }

    /// <summary>The catalogue's function is called once, with models already wrapped over the instance's.</summary>
    [Test]
    public void TheCataloguesFunction_IsCalledOnce_WithTheWrappedModels()
    {
        var provider = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)));
        var calls = new List<ToolModels>();

        var host = Host(provider, calls.Add);

        Assert.Multiple(() =>
        {
            Assert.That(calls, Has.Count.EqualTo(1));
            Assert.That(calls[0].Instance, Is.Not.SameAs(provider), "wrapped, not the provider's own client");
            Assert.That(host.Engines.Catalog.Packs.Select(pack => pack.Name), Does.Contain(ReadSamplePack.PackName));
        });
    }

    /// <summary>
    /// The pack's tool sends one request with its instructions, the photo as its bytes and kind, and the record's
    /// schema, gives back the record as text, and the ledger has one row, the tool's.
    /// </summary>
    [Test]
    public async Task ThePacksTool_ReadsThePhoto_ThroughItsModel_AndTheLedgerHasTheToolsRow()
    {
        var provider = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, Answer)));

        var (text, turn) = await ReadAsync(Host(provider));

        var (messages, options) = provider.Requests.Single();
        var contents = messages.SelectMany(message => message.Contents).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(contents.OfType<TextContent>().Select(c => c.Text), Does.Contain(ReadSampleTools.Instructions));
            Assert.That(contents.OfType<DataContent>().Single().MediaType, Is.EqualTo("image/jpeg"));
            Assert.That((options?.ResponseFormat as ChatResponseFormatJson)?.Schema?.ToString(),
                Does.Contain("accountNumber").IgnoreCase.And.Contain("totalDue").IgnoreCase);
            Assert.That(text, Does.Contain("9876543210"));
            Assert.That(turn.Records.Select(row => row.Purpose), Is.EqualTo(new[] { UsagePurpose.Tool }));
        });
    }

    /// <summary>A provider that fails makes the tool say so in text, never throw.</summary>
    [Test]
    public async Task AProviderThatFails_MakesTheToolSaySo_InText() =>
        Assert.That((await ReadAsync(Host(new FailingClient(new InvalidOperationException("overloaded"))))).Text,
            Does.Contain("overloaded"));

    /// <summary>A cancelled reading stays a cancellation, so the turn stops as the user asked.</summary>
    [Test]
    public void ACancelledReading_StaysACancellation()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var host = Host(new FailingClient(new InvalidOperationException("never reached")));

        Assert.That(async () => await ReadAsync(host, cancelled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
