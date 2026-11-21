namespace LouisAgent.Api.Contracts.Admin.Overview;

/// <summary>A name and how many talks it had (F127.8): a part of the site, an identity or a tool.</summary>
public sealed record OverviewBar(string Name, int Talks);
