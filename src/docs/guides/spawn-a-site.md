# Spawn a site

> **How-to.** Make a new site on the framework's web, with a chat and an admin, from the starter. **Elsewhere:** why a site is this small in
> [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md); what the framework holds in [louis-agent.web](../projects/louis-agent.web.md);
> changing the framework itself in [Change the framework's web](change-the-web.md).

A new site starts with a chat, an admin and a plain landing page, in the framework's black and white. All it holds is its name. Everything else is the
framework's, so a fix there reaches it.

## Make one

It works inside this repository for now, since the framework isn't a package yet, and a site's project points at the framework by a relative path. So it goes
in `demos/{site}/web/{site}/`, at that depth:

```bash
dotnet new install demos/starter/web/louis-agent.starter
dotnet new louis-site -n Acme -o demos/acme/web/acme
dotnet build demos/acme/web/acme/acme.csproj
```

`-n` is the site's name. It becomes the project's namespace and the words on the page, and in lowercase the project's file name.

Then add the project to `louis-agent-solution.sln` under `demos/acme/web`, and to the list in `SolutionLayoutTests`, which holds that the solution's tree is the disk.

## Serve it

The API serves a site from the same address, so a site needs no settings of its own. A site is served by the program that references it, as
`demos/louis-agent/hosts/louis-agent.coding-api` references the code-review app. Copy that small program into `demos/acme/hosts/`, naming your identities
and packs, and reference your site from it.

## Make it yours

| You want | Do this |
|---|---|
| Your own colours | Add a partial setting Bootstrap's colour names after `web/settings/colours` in `Styles/app.scss`; `web/settings/_colours.scss` lists them. A name you leave out keeps the framework's |
| Your own rules | Add a partial and list it after the framework's in `Styles/app.scss`. Look for a variable or a partial to reuse first |
| Your own landing page | Replace `Pages/LandingPage.razor` |
| A part of the chat turned on or off | Change `Features` in `Pages/ChatPage.razor.cs`. Language needs your identity's voices, and credits and the place need a site that uses them |
| Your own links, settings or empty chat | Fill the slots on `ChatShell` in `Pages/ChatPage.razor`, as the code-review app does with its theme picker |
| The words in your language | Register your own `ChatText` in `Program.cs`, and your own `AccountText` before `AddLouisAgentAccounts()` |
| No accounts | Drop the `louis-agent.web.accounts` reference, `AddLouisAgentAccounts()`, `AccountLibrary.Assembly` from the router, `AccountLayout`, the `accounts/shell` line; the chat's menu draws the guest's ([Change the web](change-the-web.md#change-the-account-pages)) |
| No eval studio | Drop the `louis-agent.web.evals` reference, `AddLouisAgentEvals()`, `EvalLibrary.Assembly` from the router, the `evals/pages` line and the `@using LouisAgent.Web.Evals`; and `MapLouisAgentEvals` from your API program |
| A pack's screens | Nothing, for a described screen: the sidebar lists it. For a pack's own view, reference its project under `packs/web/`, add `XWebViews.AddTo(views)` in `Program.cs`, and list its Sass |

Keep what would be the same on every site out of your site, and put it in the framework instead. `FrameworkNamesNoSiteTests` and `SiteLayoutsStaySmallTests` hold that.

## Check it

`demos/starter/tests/louis-agent.starter.integration-tests` makes a site from the template and builds it, so a change that breaks the template fails there. Run it after
changing the starter:

```bash
dotnet test demos/starter/tests/louis-agent.starter.integration-tests
```

---
[Docs index](../README.md) · Related: [Change the framework's web](change-the-web.md) · [Build a pack](build-a-pack.md)
