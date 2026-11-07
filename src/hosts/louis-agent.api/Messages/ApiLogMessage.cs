using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>The API's log lines; written to stderr.</summary>
public enum ApiLogMessage
{
    [Description("Screen {Screen}: {Action} on row {Row}, done: {Done}.")]
    ScreenAction,

    [Description("Screen {Screen}: row {Row} saved, done: {Done}.")]
    ScreenRowSaved,

    [Description("AGENT_AUTH=off: no AGENT_API_KEY, so the API accepts unauthenticated requests, on purpose. "
                 + "Only expose it on localhost.")]
    NoApiKey,

    [Description("Call UseLouisAgentKey before MapLouisAgentApi: the agent behind these routes can run shell commands "
                 + "and write files, so they won't map without the key check.")]
    KeyCheckMissing,

    [Description("Turn failed in {Session}")]
    TurnFailed,

    [Description("Session {Session} opened by user {User} in tenant {Tenant}")]
    SessionOpenedBy,
}
