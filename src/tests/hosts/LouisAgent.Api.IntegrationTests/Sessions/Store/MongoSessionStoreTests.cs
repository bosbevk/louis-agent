using LouisAgent.Api.Sessions.Store.Mongo;
using LouisAgent.Api.Testing.Sessions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LouisAgent.Api.IntegrationTests.Sessions.Store;

/// <summary>
/// The MongoDB session store keeps the contract every store keeps (F43.1, <see cref="SessionStoreContract" />), on a
/// real MongoDB, and its two collections forget a chat by a TTL index on its last activity, after the retention given.
/// Runs where <c>SESSIONS_MONGO_TEST_URL</c> names a MongoDB (CI's throwaway one); each test has a database of its
/// own, dropped after it.
/// </summary>
[Category("Integration")]
public class MongoSessionStoreTests
{
    /// <summary>The setting that names the MongoDB the tests run against.</summary>
    private const string Setting = "SESSIONS_MONGO_TEST_URL";

    /// <summary>How long the tests' store keeps a chat after its last activity.</summary>
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    private MongoClient _client = null!;
    private string _database = null!;

    [SetUp]
    public void SetUp()
    {
        string? url = Environment.GetEnvironmentVariable(Setting);
        Outside.Tool(!string.IsNullOrEmpty(url), $"{Setting} isn't set: the MongoDB session store isn't tested here");
        _client = new MongoClient(url);
        _database = $"sessions_test_{Guid.NewGuid():N}";
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_client is null) return;
        await _client.DropDatabaseAsync(_database);
        _client.Dispose();
    }

    /// <summary>A store on this test's own database.</summary>
    private MongoSessionStore Store() => new(_client.GetDatabase(_database), Retention);

    /// <summary>A saved opening loads back as it was saved.</summary>
    [Test]
    public Task AnOpening_LoadsBackAsItWasSaved() => SessionStoreContract.AnOpening_LoadsBackAsItWasSaved(Store());

    /// <summary>Turns append and load in order.</summary>
    [Test]
    public Task Turns_AppendAndLoadInOrder() => SessionStoreContract.Turns_AppendAndLoadInOrder(Store());

    /// <summary>A chat the store never saw loads as none.</summary>
    [Test]
    public Task AnUnknownChat_LoadsAsNone() => SessionStoreContract.AnUnknownChat_LoadsAsNone(Store());

    /// <summary>A delete forgets the chat and its turns.</summary>
    [Test]
    public Task ADelete_ForgetsTheChatAndItsTurns() => SessionStoreContract.ADelete_ForgetsTheChatAndItsTurns(Store());

    /// <summary>A turn number the chat already keeps is refused.</summary>
    [Test]
    public Task ATurnNumberAlreadyKept_IsRefused() => SessionStoreContract.ATurnNumberAlreadyKept_IsRefused(Store());

    /// <summary>
    /// Both collections carry a TTL index on the last activity, with the retention given, so MongoDB deletes a chat
    /// and its turns together once it has been idle that long.
    /// </summary>
    [Test]
    public async Task BothCollections_ForgetAChat_ByItsLastActivity_AfterTheRetention()
    {
        await SessionStoreContract.Turns_AppendAndLoadInOrder(Store());
        var database = _client.GetDatabase(_database);

        foreach (string collection in new[] { "sessions", "turns" })
        {
            var indexes = await (await database.GetCollection<BsonDocument>(collection).Indexes.ListAsync())
                .ToListAsync();
            var ttl = indexes.SingleOrDefault(index => index.Contains("expireAfterSeconds"));

            Assert.That(ttl?["key"].AsBsonDocument.Names, Is.EqualTo(new[] { "last_activity" }), collection);
            Assert.That(ttl?["expireAfterSeconds"].ToDouble(), Is.EqualTo(Retention.TotalSeconds), collection);
        }
    }
}
