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
| Your own rules | For one of your components, an `X.razor.scss` beside its `X.razor`: the build scopes and bundles it. For a rule every page shares, a partial listed after the framework's in `Styles/app.scss`. Look for a variable or a partial to reuse first |
| Your own landing page | Replace `Pages/LandingPage.razor` |
| A part of the chat turned on or off | Change `Features` in `Pages/ChatPage.razor.cs`. Language needs your identity's voices, and credits and the place need a site that uses them |
| Your own links, settings or empty chat | Fill the slots on `ChatShell` in `Pages/ChatPage.razor`, as the code-review app does with its theme picker |
| The words in your language | Register your own `ChatText`, and your own `AccountText`, in `Program.cs` before `AddSiteAreas` (the builder only adds its English where nothing is set) |
| No accounts | Drop `.Accounts(…)` from your `WebsiteAreas.Name`, the `louis-agent.web.accounts` reference, `AccountLayout` and the `accounts/shell` line; the chat's menu draws the guest's ([Change the web](change-the-web.md#change-the-account-pages)) |
| No eval studio | Drop `.Evals()` from your `WebsiteAreas.Name`, the `louis-agent.web.evals` reference, the `evals/pages` line and the `@using LouisAgent.Web.Evals`; and `MapLouisAgentEvals` from your API program |
| Your admin | `.Admin(typeof(AdminLayout))` is the frame and its home at `/admin`; each part inside is an area you name after it, which brings its pages, its sidebar links and its card on the home: `.Overview(count => count.Tool("SendLead", "leads"))`, `.Packs()`, `.Evals()`. Drop one and its card and links go with it. An admin area without `.Admin(…)` stops the site at start-up, saying so |
| A pack's screens | Nothing, for a described screen: the packs' menu and page list it. For a pack's own view, reference its project under `packs/web/`, add `XWebViews.AddTo(views)` in the packs' lambda, `.Packs(views => XWebViews.AddTo(views))`, and list its Sass |

Your site names its areas in one place, `WebsiteAreas.Name`, which `Program.cs` hands to `AddSiteAreas` and your tests
hand it too; never build or register a framework part yourself ([ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md)).
Keep what would be the same on every site out of your site, and put it in the framework instead.
`FrameworkNamesNoSiteTests`, `SiteLayoutsStaySmallTests` and `SitesNameTheirAreasTests` hold that.

## Check it

`demos/starter/tests/louis-agent.starter.integration-tests` makes a site from the template and builds it, so a change that breaks the template fails there. Run it after
changing the starter:

```bash
dotnet test demos/starter/tests/louis-agent.starter.integration-tests
```

---
[Docs index](../README.md) · Related: [Change the framework's web](change-the-web.md) · [Build a pack](build-a-pack.md)
