# Register a sign-in provider

> **How-to.** What to do at Google, Microsoft and LinkedIn so a site can sign people in through them (F61.3), and
> which settings to fill. **Elsewhere:** how the sign-in works in [louis-agent.accounts](../projects/louis-agent.accounts.md);
> why these three and not Facebook or X in [F61](../plans/F61-accounts-and-azure.md).

A provider is on only when both its keys are set, so an instance with none has password sign-in alone, and a button
appears the moment its keys do. Each provider wants the exact address it may send the browser back to: on the PC's
Puls stack that is `http://127.0.0.1:5070/signin-{provider}`, on a site `https://{domain}/signin-{provider}`; add
both where you test both. The keys go in `config/.env.secrets`, never in a compose or yaml file.

| Provider | Where | What to make | Callback | Settings |
|---|---|---|---|---|
| Google | [console.cloud.google.com](https://console.cloud.google.com) → *APIs & Services* → *Credentials* | An *OAuth client ID* of type *Web application*; first the *OAuth consent screen* (external, the app's name, your email). While the app is in *Testing*, only the test users you list may sign in; *Publish* it for everyone | `/signin-google` | `AUTH_GOOGLE_CLIENT_ID`, `AUTH_GOOGLE_CLIENT_SECRET` |
| Microsoft | [entra.microsoft.com](https://entra.microsoft.com) → *App registrations* → *New registration* | Supported account types: *Accounts in any organizational directory and personal Microsoft accounts*; platform *Web* with the callback; then *Certificates & secrets* → a client secret (note its expiry) | `/signin-microsoft` | `AUTH_MICROSOFT_CLIENT_ID`, `AUTH_MICROSOFT_CLIENT_SECRET` |
| LinkedIn | [linkedin.com/developers](https://www.linkedin.com/developers/apps) → *Create app* (needs a LinkedIn Page to attach it to) | Under *Products*, request *Sign In with LinkedIn using OpenID Connect*; under *Auth*, the callback and the keys. LinkedIn says this product doesn't verify identities | `/signin-linkedin` | `AUTH_LINKEDIN_CLIENT_ID`, `AUTH_LINKEDIN_CLIENT_SECRET` |

Then `docker compose … up -d` the stack (the keys reach `puls` through its env files), open `/login`, and the buttons
are there. A person who signs in through a provider for the first time gets an account in the request's tenant,
confirmed, named as the provider names them; one whose email already has an account joins it. On the profile they
can link the other providers, unlink any but the last way in, and set a password of their own.

**Not built, on purpose:** Facebook Login needs Meta Business Verification of a legal entity before it serves a
website's sign-in; X closed its free API tier to new developers in February 2026, and whether "Login with X" stays
free is unconfirmed. Both are named in `ExternalProvider` and waiting; a provider is one class behind
`IExternalProvider`.

---
[Docs index](../README.md) · Related: [louis-agent.accounts](../projects/louis-agent.accounts.md) · [Guides](README.md)
