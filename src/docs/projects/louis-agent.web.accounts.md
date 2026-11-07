# louis-agent.web.accounts

> **Explanation.** The account client: the sign-in, registration, confirmation, reset and profile pages, their
> forms, the client of the API's `/account` routes, the words, the styles and the passkey script, as a Razor class
> library a site opts into. **Elsewhere:** the server side in [louis-agent.accounts](louis-agent.accounts.md); the
> framework's web it sits on in [louis-agent.web](louis-agent.web.md); how a site changes the pages in
> [Change the web](../guides/change-the-web.md). Being built as CL30 ([the plan](../plans/CL30-web-accounts.md)).

## Purpose

What the browser does for accounts, apart from the framework's web: a site that has accounts references this and
calls `AddLouisAgentAccounts()`; a site that doesn't ships none of it, and its chat header draws the guest's menu
through the seam `louis-agent.web` keeps for that.

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
