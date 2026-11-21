# Register a sign-in provider

> **How-to.** Let people sign in to a site with Google, Microsoft or LinkedIn: what to make at the provider, and
> which settings to fill. **Elsewhere:** how the sign-in works in [LouisAgent.Accounts](../projects/LouisAgent.Accounts.md);
> why these three and not Facebook or X in [F61 (history)](https://github.com/bosbevk/louis-agent/blob/20f28c9f/src/docs/plans/F61-accounts-and-azure.md).

![Signing in through Google, Microsoft or LinkedIn, each with its callback, and the keys in the secrets file](diagrams/register-a-sign-in-provider.svg)

A provider turns on only when both of its keys are set. So a site with no keys has password sign-in alone, and a
provider's button appears as soon as its keys do. You can add the providers one at a time.

## Before you start

Each provider asks for the exact address it may send the browser back to, its callback. On the PC's Puls stack that
is `http://127.0.0.1:5070/signin-{provider}`, and on a live site `https://{domain}/signin-{provider}`. If you test in
both places, add both addresses.

## Steps

![The steps: make the app at the provider, put its two keys in the secrets file, recreate the stack, and the button is on /login](diagrams/register-a-sign-in-provider-flow.svg)

1. **Make the app at the provider**, as its row in the table says, and copy its client ID and secret.
2. **Put the two keys in the demo's `config/.env.secrets`**, under the names in the table. Never put them in a
   compose or YAML file.
3. **Recreate the stack** with `docker compose … up -d`. The keys reach `puls` through its env files.
4. **Open `/login`.** The provider's button is there.

| Provider | Where | What to make | Callback | Settings |
|---|---|---|---|---|
| Google | [console.cloud.google.com](https://console.cloud.google.com) → *APIs & Services* → *Credentials* | First the *OAuth consent screen* (external, the app's name, your email), then an *OAuth client ID* of type *Web application*. While the app is in *Testing*, only the test users you list can sign in. *Publish* it to open it to everyone | `/signin-google` | `AUTH_GOOGLE_CLIENT_ID`, `AUTH_GOOGLE_CLIENT_SECRET` |
| Microsoft | [entra.microsoft.com](https://entra.microsoft.com) → *App registrations* → *New registration* | Supported account types: *Accounts in any organizational directory and personal Microsoft accounts*. Platform *Web*, with the callback. Then *Certificates & secrets* → a client secret, and note when it expires | `/signin-microsoft` | `AUTH_MICROSOFT_CLIENT_ID`, `AUTH_MICROSOFT_CLIENT_SECRET` |
| LinkedIn | [linkedin.com/developers](https://www.linkedin.com/developers/apps) → *Create app* (it needs a LinkedIn Page to belong to) | Under *Products*, request *Sign In with LinkedIn using OpenID Connect*. Under *Auth*, the callback and the keys. LinkedIn says this product doesn't verify identities | `/signin-linkedin` | `AUTH_LINKEDIN_CLIENT_ID`, `AUTH_LINKEDIN_CLIENT_SECRET` |

## What happens when someone signs in

The first time someone signs in through a provider, they get an account in the tenant the request came in on. It is
already confirmed, and it carries the name the provider gives them. If their email already has an account, the
provider joins that account instead. On their profile they can link the other providers, unlink any provider except
their last way in, and set a password of their own.

## Facebook and X

These two are left out on purpose. Facebook Login needs Meta Business Verification of a legal entity before it serves
a website's sign-in. X closed its free API tier to new developers in February 2026, and whether "Login with X" stays
free is unconfirmed. Both are already named in `ExternalProvider`, and adding a provider is one class behind
`IExternalProvider`.

---
[Docs index](../README.md) · Related: [LouisAgent.Accounts](../projects/LouisAgent.Accounts.md) · [Guides](README.md)
