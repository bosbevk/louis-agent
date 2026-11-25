using System.Globalization;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Api.Config;

/// <summary>
/// Where the chats are kept, and for how long after their last message (F43.1). Unset, they stay in memory; a setting
/// that can't be read stops the host as it starts, saying which, so a typo never quietly keeps chats in memory.
/// </summary>
/// <param name="Store">SESSION_STORE: memory, the default, or mongo.</param>
/// <param name="Url">SESSION_STORE_URL: the MongoDB's address, which mongo needs.</param>
/// <param name="Retention">SESSION_RETENTION: how long an idle chat is kept, 30 days when unset.</param>
/// <remarks>
/// A plain options record with a reader that takes the variable lookup, as <see cref="OpenDoorOptions"/> is, so a test
/// needs no real environment. It fails loud where the door fails shut: a door that can't read its cap stays closed,
/// which is safe, while a store that can't be read would lose every chat on the next restart without a word.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public sealed record SessionStoreOptions(SessionStoreKind Store, string? Url, TimeSpan Retention)
{
    /// <summary>The setting that picks where the chats are kept.</summary>
    public const string StoreSetting = "SESSION_STORE";

    /// <summary>The setting that names the MongoDB.</summary>
    public const string UrlSetting = "SESSION_STORE_URL";

    /// <summary>The setting that says how long an idle chat is kept.</summary>
    public const string RetentionSetting = "SESSION_RETENTION";

    /// <summary>How long an idle chat is kept when the setting is unset.</summary>
    public static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(30);

    /// <summary>Reads the three settings; the variable lookup is replaceable for tests.</summary>
    public static SessionStoreOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;

        // A blank setting is the same as a missing one; anything else comes back trimmed.
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        var store = StoreOf(Get(StoreSetting));
        string? url = Get(UrlSetting);
        if (store == SessionStoreKind.Mongo && url is null)
            throw new ArgumentException(MessageText.Format(ApiErrorMessage.SessionStoreUrlMissing));
        return new SessionStoreOptions(store, url, RetentionOf(Get(RetentionSetting)));
    }

    /// <summary>The store named, memory when none is.</summary>
    private static SessionStoreKind StoreOf(string? setting)
    {
        if (setting is null) return SessionStoreKind.Memory;
        if (SettingChoice.TryParse(setting, out SessionStoreKind? store)) return store!.Value;
        throw new ArgumentException(MessageText.Format(ApiErrorMessage.UnknownSessionStore));
    }

    /// <summary>The retention given, as .NET writes a duration, or 30 days; it must be above zero.</summary>
    private static TimeSpan RetentionOf(string? setting)
    {
        if (setting is null) return DefaultRetention;
        if (TimeSpan.TryParse(setting, CultureInfo.InvariantCulture, out var retention) && retention > TimeSpan.Zero)
            return retention;
        throw new ArgumentException(MessageText.Format(ApiErrorMessage.SessionRetentionInvalid));
    }
}
