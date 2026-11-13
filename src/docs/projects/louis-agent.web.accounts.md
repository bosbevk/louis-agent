# louis-agent.web.accounts — design

> **Kind:** Razor class library (`net10.0`, runs in the browser) · **Path:** `src/web/louis-agent.web.accounts/` ·
> **References:** louis-agent.web, louis-agent.accounts.contracts, Microsoft.AspNetCore.Components.Authorization ·
> **Used by:** a site that has accounts: louis-agent.puls, louis-agent.starter, louis-agent.code-review

![The account pages, and their parts: the pages, the client and passkeys](diagrams/louis-agent.web.accounts.svg)

## Purpose

What the browser does for accounts, apart from the framework's web (CL30, [ADR 0089](../adr/0089-the-account-client-is-a-library-a-site-opts-into.md)):
the sign-in, registration, confirmation, reset and profile pages, their forms and sections, the client of the API's
`/account` routes, the words, the styles and the passkey script. A site that has accounts references this and calls
`AddLouisAgentAccounts()`, as its API references `louis-agent.accounts`; a site that doesn't ships none of it, and its
chat header draws the guest's menu through the one seam `louis-agent.web` keeps for that. The server side is
[louis-agent.accounts](louis-agent.accounts.md).

## How it works

- **The client** (`Client/`). `AccountApi` talks to the API's `/account` routes (Identity's and ours), every call
  marked as the app's own (`X-Requested-With`), which is what lets a signed-in person's cookie count in place of a
  key; a refusal is an `AccountApiException` carrying the route's `AccountProblem`. `AccountState` is the one truth
  about who is signed in: the pages, the menu and Blazor's `AuthorizeView` (through `CookieAuthenticationStateProvider`)
  follow its `Changed`. `AccountServices.AddLouisAgentAccounts()` registers them, the English `AccountText` unless the
  site registered its own first, and the chat menu's seam.
- **The seam** (`Client/Menu/AccountMenu`). The framework's web declares `IAccountMenu` beside its profile menu: are
  there accounts, who is signed in, the two labels, load, changed, sign out. This library implements it over the
  state, the client and the words; without the library the menu holds `NoAccounts`, a Null Object, and draws the
  guest's menu. Separated Interface (Fowler): the interface where it's needed, the implementation where it exists, so
  the dependency points the way the libraries ship.
- **The pages** (`Pages/`) are framework pages on `AccountFrame`, which draws inside the layout the site set in
  `SiteFrames.Account` (the Bridge the admin's frame uses too); `AccountShell` is that frame, one centred card with
  the site's brand. The forms (`Components/`) and the profile's sections (`Components/Profile/`, `Passkeys/`,
  `TwoFactor/`) each do one thing over the client, show what went wrong in the site's words (`AccountErrors`, a
  switch on Identity's codes), and read again after every write (ADR 0042).
- **Passkeys** (`Components/Passkeys/`, `wwwroot/js/passkeys.js`): a module (CL31) with three functions the two components import
  through `JsModule` (`Scripts/AccountScripts` names its path), which hand the API's options to `navigator.credentials` and the browser's credential back, untouched; a browser without
  the parsing helpers sees no form. **Two-factor** (`Components/TwoFactor/`, `Client/TwoFactor/`): the key as text
  and an `otpauth://` link with the site's host as the issuer (`OtpAuth`), and the sign-in form's one second box,
  digits as the app's code and the rest as a recovery code (`SecondStep`).
- **The words** are `AccountText` (`Text/`), English; a site registers its own copy before `AddLouisAgentAccounts()`
  (Puls: `PulsAccountText.Afrikaans`). **The styles** sit beside their components since CL58
  ([ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md)): the card and, under `::deep`, what every page draws
  inside it in `AccountShell.razor.scss`, the notice's, the password field's, the providers' divider's, the facts' and
  the key's in their own files; scoped and bundled by the build, so a site lists nothing of them; colours from
  variables only.

## Diagrams

![louis-agent.web.accounts: what a site uses, and what the library uses in the framework's web](diagrams/louis-agent.web.accounts-diagrams.svg)

![The profile menu: with no accounts it shows the guest's menu, otherwise it asks /account/me and shows the person](diagrams/louis-agent.web.accounts-diagrams-2.svg)

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

`src/tests/web/louis-agent.web.accounts.tests`: the client's logic with plain NUnit (the error words, the return
address, the authenticator link, the second step), the components and pages with bUnit over an API the test scripts
(`AccountTestContext`, every request kept), the passkey components with bUnit's JS interop in strict mode, the seam's
real side (`AccountMenuTests`). The framework's rules read this library too: it names no site, its Razor holds no code,
its styles hold no fixed colour, and every passkey call is in its script (`louis-agent.web.tests/Rules`).

## Limits and plans

The QR code for the authenticator key, a passkey's name edited after the fact, and the pages seen in a browser wait.

## Related docs

[louis-agent.web](louis-agent.web.md) · [louis-agent.accounts](louis-agent.accounts.md) ·
[Change the web](../guides/change-the-web.md) · [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md) ·
[ADR 0089](../adr/0089-the-account-client-is-a-library-a-site-opts-into.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
