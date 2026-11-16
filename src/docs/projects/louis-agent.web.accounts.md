# louis-agent.web.accounts — design

> **Kind:** Razor library (`net10.0`, runs in the browser) · **Path:** `src/web/louis-agent.web.accounts/` ·
> **References:** louis-agent.web, louis-agent.accounts.contracts, Microsoft.AspNetCore.Components.Authorization ·
> **Used by:** a site that has accounts: louis-agent.puls, louis-agent.starter, louis-agent.code-review
> **Also shows:** `component`, `sequence`

![The account pages, and their parts: the pages, the client and passkeys](diagrams/louis-agent.web.accounts.svg)

## Purpose

This library is everything the browser does for accounts. It has the pages for signing in, registering, confirming
an email, resetting a password and editing your profile, with their forms and sections. It also has the client that
talks to the API's `/account` routes, the words, the styles and the passkey script.

It sits apart from the framework's web on purpose. A site that has accounts references it and calls
`AddLouisAgentAccounts()`, just as its API references `louis-agent.accounts`. A site without accounts ships none of
it, and its chat header simply draws the guest's menu, through the one seam `louis-agent.web` keeps for that. The
server side is [louis-agent.accounts](louis-agent.accounts.md).

*Decided in:* CL30, [ADR 0089](../adr/0089-the-account-client-is-a-library-a-site-opts-into.md).

## How it works

The profile page is a good example of how the pages are put together. It's one page with a section for each kind of
change, and each section asks the account API for its own data.

![The profile page holds a section for each change, hands each the person signed in, and the last says back when the account is gone](diagrams/louis-agent.web.accounts-components-profile.svg)

### 1. The client

`AccountApi`, in `Client/`, talks to the API's `/account` routes, both Identity's and ours. Every call is marked as
the app's own with `X-Requested-With`. That marker is what lets a signed-in person's cookie count instead of a key.
When the API says no, the client throws an `AccountApiException` carrying the route's `AccountProblem`.

`AccountState` is the one truth about who's signed in. The pages, the menu and Blazor's `AuthorizeView` (through
`CookieAuthenticationStateProvider`) all follow its `Changed` event. `AccountServices.AddLouisAgentAccounts()`
registers all of this, plus the chat menu's seam, plus the English `AccountText`, unless the site registered its own
words first.

### 2. The seam the framework keeps

The framework's web declares `IAccountMenu` next to its profile menu. It asks: are there accounts, who's signed in,
what are the two labels, and it offers load, changed and sign out. This library implements it, in
`Client/Menu/AccountMenu`, over the state, the client and the words.

Without this library, the menu holds `NoAccounts`, a Null Object (a stand-in that does nothing), and draws the
guest's menu. This is the Separated Interface pattern from Fowler: the interface lives where it's needed, the
implementation lives where it exists. That way the dependency points the same way the libraries ship.

### 3. The pages

The pages, in `Pages/`, are framework pages on `AccountFrame`. That frame draws inside whatever layout the site set
in `SiteFrames.Account`, the same Bridge the admin's frame uses. `AccountShell` is the frame itself: one centred card
with the site's brand.

The forms (`Components/`) and the profile's sections (`Components/Profile/`, `Passkeys/`, `TwoFactor/`) each do one
thing over the client. When something goes wrong, they say so in the site's own words, through `AccountErrors`, a
switch on Identity's codes. And after every write, they read again, so what you see is what's really stored.

*Decided in:* [ADR 0042](../adr/0042-the-web-shows-the-live-results.md).

### 4. Passkeys

Passkeys live in `Components/Passkeys/` and `wwwroot/js/passkeys.js`. The script is a module with three functions,
which the two components import through `JsModule` (`Scripts/AccountScripts` names its path). They pass the API's
options to `navigator.credentials`, and hand the browser's credential back, untouched. A browser without the parsing
helpers just doesn't see the form.

*Decided in:* CL31.

### 5. Two-factor

Two-factor lives in `Components/TwoFactor/` and `Client/TwoFactor/`. It shows the key as text, plus an
`otpauth://` link with the site's host as the issuer (`OtpAuth`). The sign-in form has one second box for it
(`SecondStep`): digits are read as the app's code, and anything else as a recovery code.

### 6. Words and styles

The words are `AccountText`, in `Text/`, in English. A site can register its own copy before
`AddLouisAgentAccounts()`; Puls does, with `PulsAccountText.Afrikaans`.

The styles sit beside their components. `AccountShell.razor.scss` holds the card, and under `::deep`, what every page
draws inside it. The notice, the password field, the providers' divider, the facts and the key each have their own
file. The build scopes and bundles them, so a site lists none of them, and colours only ever come from variables.

No sheet here draws a surface itself. The card is a pane, a box you type in is a field, the main button is a press
and the second one a quiet press, each included from the framework's surface tool
([the framework's web](louis-agent.web.md)). So the account pages wear whatever theme the site's page wears, and on
a site that names no theme they keep the site's own colours, as Puls's do.

*Decided in:* CL58, [ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md).

![louis-agent.web.accounts: what a site uses, and what the library uses in the framework's web](diagrams/louis-agent.web.accounts-diagrams.svg)

![The profile menu: with no accounts it shows the guest's menu, otherwise it asks /account/me and shows the person](diagrams/louis-agent.web.accounts-diagrams-2.svg)

These are the types involved:

![The types: one call adds the pages, a client for the accounts API, the state every page reads, and the framework's account corner filled in](diagrams/louis-agent.web.accounts-interface.svg)

## Structure

```
src/web/louis-agent.web.accounts/
├── Client/              # AccountApi, AccountApiException, AccountProblem, AccountState, CookieAuthenticationStateProvider, AccountServices;
│                        # Errors/ (AccountErrors), Navigation/ (SafeReturn), TwoFactor/ (OtpAuth, SecondStep), Menu/ (AccountMenu)
├── Components/          # AccountShell, AccountNotice, PasswordField, ProviderButtons, LoginForm, RegisterForm;
│                        # Profile/ (details, email, password, set password, linked logins, delete), Passkeys/ (list, sign in), TwoFactor/ (section)
├── Layout/              # AccountFrame, on SiteFrames.Account
├── Pages/               # login, register, confirm-email, forgot-password, reset-password, profile
├── Text/                # AccountText
├── _Imports.razor
└── wwwroot/js/          # passkeys.js, a module served at _content/louis-agent.web.accounts/js/passkeys.js; no site lists it
src/tests/web/louis-agent.web.accounts.tests/   # the same folders; AccountTestContext scripts the API
```

## Tests

The tests are in `src/tests/web/louis-agent.web.accounts.tests`. The client's logic runs on plain NUnit: the error
words, the return address, the authenticator link and the second step. The components and pages run on bUnit, over
an API the test scripts (`AccountTestContext`, which keeps every request). The passkey components run on bUnit's JS
interop in strict mode. And `AccountMenuTests` covers the seam's real side.

The framework's own rules, in `louis-agent.web.tests/Rules`, read this library too. They check that it names no
site, that its Razor holds no code, that its styles hold no fixed colour, and that every passkey call is in its
script.

## Limits and plans

Three things are still waiting: a QR code for the authenticator key, editing a passkey's name after it's made, and
looking at the pages in a real browser.

## Related docs

[louis-agent.web](louis-agent.web.md) · [louis-agent.accounts](louis-agent.accounts.md) ·
[Change the web](../guides/change-the-web.md) · [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md) ·
[ADR 0089](../adr/0089-the-account-client-is-a-library-a-site-opts-into.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
