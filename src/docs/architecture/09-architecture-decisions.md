# 9. Architecture decisions

> **Explanation.** Every lasting decision is an ADR, one record each, with its context and what it cost. **Elsewhere:**
> the list, the rules and the template in [architecture decisions](../adr/README.md).

A handful of decisions shape the most, and they read best in this order.

Start with [0002](../adr/0002-messages-api-tier-with-our-own-loop.md), our own loop behind `IChatClient`, and
[0003](../adr/0003-provider-strategies.md), provider strategies. Together they explain why the provider is just
configuration.

Then [0006](../adr/0006-toolsets-per-session.md), toolsets per session, and
[0008](../adr/0008-history-is-append-only.md), append-only history. They're why a session's tools and prompt never
change once it starts.

[0007](../adr/0007-measure-every-request.md), measure every request, is why every change is proven on the ledger. And
[0004](../adr/0004-arguments-as-data.md), arguments as data, with [0005](../adr/0005-guard-the-context.md), the context
guards, are why model text can't become a command or flood the context.
