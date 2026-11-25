using LouisAgent.Api.Config;

namespace LouisAgent.Api.Tests.Config;

/// <summary>
/// Where the chats are kept, and for how long (F43.1): SESSION_STORE picks memory or MongoDB, SESSION_STORE_URL names
/// the MongoDB, SESSION_RETENTION how long an idle chat is kept. A setting that can't be read stops the host as it
/// starts, saying which, so a typo never quietly keeps chats in memory.
/// </summary>
public class SessionStoreOptionsTests
{
    /// <summary>The settings as a lookup, the rest unset.</summary>
    private static Func<string, string?> Settings(string? store, string? url = null, string? retention = null) =>
        name => name switch
        {
            SessionStoreOptions.StoreSetting => store,
            SessionStoreOptions.UrlSetting => url,
            SessionStoreOptions.RetentionSetting => retention,
            _ => null
        };

    /// <summary>Unset or blank, chats are kept in memory, for 30 days.</summary>
    [TestCase(null)]
    [TestCase("  ")]
    [TestCase("memory")]
    [TestCase("Memory")]
    public void UnsetOrMemory_KeepsChatsInMemory_For30Days(string? store)
    {
        var options = SessionStoreOptions.FromEnvironment(Settings(store));

        Assert.Multiple(() =>
        {
            Assert.That(options.Store, Is.EqualTo(SessionStoreKind.Memory));
            Assert.That(options.Retention, Is.EqualTo(TimeSpan.FromDays(30)));
        });
    }

    /// <summary>MongoDB with its address, and a retention given as a .NET duration.</summary>
    [Test]
    public void Mongo_ReadsItsAddressAndTheRetention()
    {
        var options = SessionStoreOptions.FromEnvironment(
            Settings("mongo", " mongodb://localhost:27017/louis_agent ", "7.00:00:00"));

        Assert.Multiple(() =>
        {
            Assert.That(options.Store, Is.EqualTo(SessionStoreKind.Mongo));
            Assert.That(options.Url, Is.EqualTo("mongodb://localhost:27017/louis_agent"));
            Assert.That(options.Retention, Is.EqualTo(TimeSpan.FromDays(7)));
        });
    }

    /// <summary>
    /// A store that is neither, MongoDB with no address, or a retention that isn't a positive duration.
    /// </summary>
    [TestCase("redis", null, null, SessionStoreOptions.StoreSetting)]
    [TestCase("mongo", null, null, SessionStoreOptions.UrlSetting)]
    [TestCase("mongo", "  ", null, SessionStoreOptions.UrlSetting)]
    [TestCase("memory", null, "a month", SessionStoreOptions.RetentionSetting)]
    [TestCase("memory", null, "00:00:00", SessionStoreOptions.RetentionSetting)]
    [TestCase("memory", null, "-1.00:00:00", SessionStoreOptions.RetentionSetting)]
    public void ASettingThatCantBeRead_StopsTheHost_NamingIt(string? store, string? url, string? retention,
        string named)
    {
        Assert.That(() => SessionStoreOptions.FromEnvironment(Settings(store, url, retention)),
            Throws.ArgumentException.With.Message.Contains(named));
    }
}
