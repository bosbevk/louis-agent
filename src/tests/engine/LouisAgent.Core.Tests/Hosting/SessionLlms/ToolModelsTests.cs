using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Hosting.SessionLlms;

/// <summary>
/// F171.1 step 7: the models a pack's tool may be given, the instance's and each profile's, each wrapped once, so a
/// tool's request is recorded in the ledger as the tool's, inside the turn it ran in, and a profile is picked by name.
/// </summary>
public class ToolModelsTests
{
    /// <summary>The model the instance runs on, as the ledger names it.</summary>
    private const string InstanceModel = "claude-sonnet-5-5";

    /// <summary>The model the <c>haiku</c> profile runs on.</summary>
    private const string HaikuModel = "claude-haiku-5-5";

    /// <summary>A model that answers once, naming itself as the provider would.</summary>
    private static FakeChatClient Model(string id) =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "read")) { ModelId = id });

    /// <summary>
    /// The models of an instance with one profile, <c>haiku</c>, writing to <paramref name="ledger" />.
    /// </summary>
    private static ToolModels Models(FakeChatClient instance, FakeChatClient haiku, ListUsageSink ledger) =>
        ToolModels.Create(instance, null, [new SessionLlm("haiku", haiku)], ledger);

    /// <summary>A request through the instance's model reaches the instance's client.</summary>
    [Test]
    public async Task ARequestThroughTheInstance_ReachesItsClient()
    {
        var instance = Model(InstanceModel);
        var haiku = Model(HaikuModel);

        await Models(instance, haiku, new ListUsageSink()).Instance.GetResponseAsync("Read this");

        Assert.Multiple(() =>
        {
            Assert.That(instance.Requests, Has.Count.EqualTo(1));
            Assert.That(haiku.Requests, Is.Empty);
        });
    }

    /// <summary>
    /// Inside a turn, a tool's request is recorded as the tool's, and keeps the turn's host, session and turn.
    /// </summary>
    [Test]
    public async Task InsideATurn_ItsRowIsTheTools_AndKeepsTheTurn()
    {
        var ledger = new ListUsageSink();
        var models = Models(Model(InstanceModel), Model(HaikuModel), ledger);

        using (UsageScope.Begin(UsagePurpose.Turn, host: "api", session: "s-1", turn: 3))
            await models.Instance.GetResponseAsync("Read this");

        var row = ledger.Records.Single();
        Assert.Multiple(() =>
        {
            Assert.That(row.Purpose, Is.EqualTo(UsagePurpose.Tool));
            Assert.That((row.Host, row.Session, row.Turn), Is.EqualTo(("api", "s-1", (int?)3)));
        });
    }

    /// <summary>A streamed request is recorded as the tool's too.</summary>
    [Test]
    public async Task AStreamedRequest_IsTheToolsToo()
    {
        var ledger = new ListUsageSink();
        var models = Models(Model(InstanceModel), Model(HaikuModel), ledger);

        using (UsageScope.Begin(UsagePurpose.Turn, host: "api", session: "s-1", turn: 3))
            await foreach (var _ in models.Instance.GetStreamingResponseAsync("Read this"))
            {
            }

        Assert.That(ledger.Records.Single().Purpose, Is.EqualTo(UsagePurpose.Tool));
    }

    /// <summary>
    /// A request through a profile by name reaches that profile's client, and its row names its model.
    /// </summary>
    [Test]
    public async Task ARequestThroughAProfile_ReachesItsClient_AndItsRowNamesItsModel()
    {
        var ledger = new ListUsageSink();
        var instance = Model(InstanceModel);
        var haiku = Model(HaikuModel);

        await Models(instance, haiku, ledger).Named("haiku").GetResponseAsync("Read this");

        Assert.Multiple(() =>
        {
            Assert.That(haiku.Requests, Has.Count.EqualTo(1));
            Assert.That(instance.Requests, Is.Empty);
            Assert.That(ledger.Records.Single().Model, Is.EqualTo(HaikuModel));
        });
    }

    /// <summary>A name that isn't a profile stops at once, naming the profiles there are.</summary>
    [Test]
    public void ANameThatIsntAProfile_Throws_NamingTheProfiles() =>
        Assert.That(() => Models(Model(InstanceModel), Model(HaikuModel), new ListUsageSink()).Named("haiku5"),
            Throws.InstanceOf<SessionLlmException>().With.Message.Contains("haiku5").And.Message.Contains("haiku"));
}
