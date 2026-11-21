namespace LouisAgent.Packs.Leads.Sending;

/// <summary>
/// Someone who wants a site with an agent, or a custom one (F112): who they are, how the owner may reach them, what
/// they want in their own words, and when they said so, in UTC; and, since F127.2, the identity that took it and
/// whether that identity is a demonstration, so a try-out's booking is marked a demo.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0046-dates-are-utc.md">ADR 0046: dates are UTC</seealso>
public sealed record Lead(
    string Name,
    string Contact,
    string Wants,
    DateTimeOffset At,
    string? Identity = null,
    bool Demonstration = false);
