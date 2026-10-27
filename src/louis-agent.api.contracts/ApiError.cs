namespace LouisAgent.Api.Contracts;

/// <summary>What every error looks like on the wire: <c>{"type":"error","error":{"type":…,"message":…}}</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ApiError(string Type, ApiErrorDetail Error);

/// <summary>The error's own kind and the words that say what went wrong.</summary>
public sealed record ApiErrorDetail(string Type, string Message);
