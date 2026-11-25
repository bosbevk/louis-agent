using LouisAgent.Choices;

namespace LouisAgent.Api.Config;

/// <summary>Where the chats are kept (F43.1), as SESSION_STORE names it.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum SessionStoreKind
{
    /// <summary>In this process only: a restart ends every chat.</summary>
    [WireName("memory")] Memory,

    /// <summary>In MongoDB, so a chat survives a restart.</summary>
    [WireName("mongo")] Mongo,
}
