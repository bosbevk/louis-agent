# Spawn a site

> **How-to.** Make a new site on the framework's web, with a chat and an admin, from the starter. **Elsewhere:** why
> a site is this small in [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md); what the framework
> holds in [LouisAgent.Web](../projects/LouisAgent.Web.md); changing the framework itself in
> [Change the framework's web](change-the-web.md).

![A new site: made from the starter template, it holds only its name and the areas it names, and the framework holds everything else](diagrams/spawn-a-site.svg)

A new site starts with a chat, an admin and a plain landing page, in the framework's black and white. All it holds is
its name. Everything else is the framework's, so a fix there reaches it.

## Make one

![Making a site: install the template, make the site, build it, add it to the solution, then serve it from a program](diagrams/spawn-a-site-flow.svg)

For now it works only inside this repository. The framework isn't a package yet, so a site's project points at the
framework by a relative path, and the site has to sit at `demos/{site}/web/{site}/`, at that depth.

1. **Install the template, make the site and build it:**

   ```bash
   dotnet new install demos/starter/web/LouisAgent.Starter
   dotnet new louis-site -n Acme -o demos/acme/web/acme
   dotnet build demos/acme/web/acme/acme.csproj
   ```

   `-n` is the site's name. It becomes the project's namespace and the words on the page, and, in lowercase, the
   project's file name.

2. **Add the project to `louis-agent-solution.sln`** under `demos/acme/web`, and to the list in `SolutionLayoutTests`,
   which holds that the solution's tree is the disk.
3. **Serve it from a program.** The API serves a site from its own address, so a site needs no settings of its own. A
   site is served by the program that references it, as `demos/louis-agent/hosts/LouisAgent.CodingApi` references
   the code-review app. Copy that small program into `demos/acme/hosts/`, name your identities and packs in it, and
   reference your site from it.

The site it makes offers two themes, glass and flat, and opens in glass, light. A visitor picks another with the
switch in the chat's settings or at the foot of the admin's sidebar. The browser remembers it, so the next page and the
next visit open the same way before the page is first drawn.

## Make it yours

The chat page it makes is the framework's chat shell, with your site's name in its logo slot. The other slots are
yours to fill:

![A new site's chat page holds the framework's chat shell with the site's name in its logo slot](diagrams/spawn-a-site-components-chat.svg)

| You want | Do this |
|---|---|
| Your own colours | Add a partial that sets Bootstrap's colour names, after `web/settings/colours`, `web/settings/surfaces` and the themes' sheets in `Styles/app.scss`. `web/settings/_colours.scss` lists them. A name you leave out keeps the framework's |
| Your themes | The site offers glass, then flat, each light and dark: `.Glass().Flat()` in your `WebsiteAreas.Name`, the `LouisAgent.Web.Themes` reference, its `Styles` in `sasscompiler.json`, and the four `theme-…` lines in `Styles/app.scss`. The first theme named is the default, and `wwwroot/index.html` writes it as `data-theme` (`IndexLookTests` holds it for louis-agent.dev). To offer one, drop the other's call and its two lines. To offer none, drop all of it and the `data-theme` |
| Your own rules | For one of your components, an `X.razor.scss` beside its `X.razor`: the build scopes and bundles it. For a rule every page shares, a partial listed after the framework's in `Styles/app.scss`. Look for a variable or a partial to reuse first |
| Your own landing page | Replace `Pages/LandingPage.razor` |
| A part of the chat turned on or off | Change `Features` in `Pages/ChatPage.razor.cs`. Language needs your identity's voices, and credits and the place need a site that uses them |
| Your own links, settings or empty chat | Fill the slots on `ChatShell` in `Pages/ChatPage.razor`, as the code-review app does with its theme picker |
| The words in your language | Register your own `ChatText`, and your own `AccountText`, in `Program.cs` before `AddSiteAreas`. The builder only adds its English where nothing is set |
| No accounts | Drop `.Accounts(…)` from your `WebsiteAreas.Name`, the `LouisAgent.Web.Accounts` reference, `AccountLayout` and the two `@using LouisAgent.Web.Accounts…` lines in `_Imports.razor`. The chat's menu then draws the guest's ([Change the web](change-the-web.md#change-the-account-pages)) |
| No eval studio | Drop `.Evals()` from your `WebsiteAreas.Name`, the `LouisAgent.Web.Evals` reference, the `evals/pages` line and the `@using LouisAgent.Web.Evals`, and `MapLouisAgentEvals` from your API program |
| Your admin | `.Admin(typeof(AdminLayout))` is the frame and its home at `/admin`. Each part inside is an area you name after it, which brings its pages, its sidebar links and its card on the home: `.Overview(count => count.Tool("SendLead", "leads"))`, `.Packs()`, `.Evals()`. Drop one and its card and links go with it. An admin area without `.Admin(…)` stops the site at start-up, and says so |
| A pack's screens | Nothing, for a described screen: the packs' menu and page list it. For a pack's own view, reference its project under `packs/web/`, add `XWebViews.AddTo(views)` in the packs' lambda, `.Packs(views => XWebViews.AddTo(views))`, and list its Sass |

## Name your areas in one place

Your site names its areas in one place, `WebsiteAreas.Name`. `Program.cs` hands it to `AddSiteAreas`, and your tests
hand it over too. Never build or register a framework part yourself
([ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md)). Keep whatever would be the same on every site
out of your site, and put it in the framework instead. `FrameworkNamesNoSiteTests`, `SiteLayoutsStaySmallTests` and
`SitesNameTheirAreasTests` hold that.

## Check it

`demos/starter/tests/LouisAgent.Starter.IntegrationTests` makes a site from the template and builds it, so a change
that breaks the template fails there. Run it after you change the starter:

```bash
dotnet test demos/starter/tests/LouisAgent.Starter.IntegrationTests
```

---
[Docs index](../README.md) · Related: [Change the framework's web](change-the-web.md) · [Build a pack](build-a-pack.md)
