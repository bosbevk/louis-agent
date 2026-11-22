# Change the site

> **How-to.** Change `src/site/LouisAgent.Site`, louis-agent.dev, the website that sells websites with agents: run
> it, add a page, change its look, change the CV, draw the share card again, and test it. **Elsewhere:** how it works
> in [LouisAgent.Site](../projects/LouisAgent.Site.md); what every site shares in
> [Change the framework's web](change-the-web.md); why it lives in `src/site/` in
> [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md).

![The site at a glance: LouisAgent.Site is configured framework components, served by LouisAgent.SiteApi with the guide in the corner, and its tests hold every page to the repo](diagrams/change-the-site.svg)

The site is Blazor WebAssembly on the framework's web. Its host, `LouisAgent.SiteApi`, serves the pages as HTML and
the API the guide's chat runs on, and its catalogue holds the guide and nothing else. The framework's own pages are not
here any more: they are the framework's site, [LouisAgent.SiteFramework](../projects/LouisAgent.SiteFramework.md).

The thing to know before you change anything is that this site holds no HTML and no styles of its own. A page is the
framework's controls given the site's words and data
([ADR 0125](../adr/0125-a-site-is-configured-framework-components.md)), and its look is the theme's. So most changes
here are words, and a new shape or a new look is built in the framework first.

## Run it

```bash
docker compose -f src/site/docker/docker-compose.yml up -d --build      # http://127.0.0.1:5080, the review copy
dotnet run --project src/site/LouisAgent.SiteApi                      # http://127.0.0.1:5083
```

Its settings are in `src/site/config/` ([what each holds](../../site/config/README.md)). Copy `.env.secrets.example`
to `.env.secrets` and fill in `AGENT_API_KEY`, or the host refuses to start, and `ANTHROPIC_API_KEY`, or the guide
can't answer. After a change to the site, rebuild the image or restart the run.

## Add a page

![Adding a page: write it from the framework's controls, put it on the menu, give the guide its address, then hold it with the tests and write the sitemap again](diagrams/change-the-site-flow.svg)

A page is a `.razor` under `Pages/`, in the folder of what it is about (`About/`, `Agents/`, `Offers/`). The privacy
page, `Pages/About/PrivacyPage.razor`, is a good one to copy: a `Band` per section and the framework's tiles inside.

1. **Write the page.** It opens with `@page "/its-route"`, a `<PageTitle>` and a `<PageHead>` whose description is its
   own. After that it holds only the framework's controls, given its words, and no `@code`. Every section is a `Band`
   with an `Id` in words, so it has its own address and the guide can take a visitor there
   ([Make a page move](change-the-web.md#make-a-page-move)). It draws in the site's frame by default.
2. **Put it on the menu**, as a `NavLink` in `Layout/SiteLayout.razor`, or inside a `SiteMenu` when it belongs to a
   group, as the demos do.
3. **Tell the guide.** Its voice and skills (`src/site/LouisAgent.Identities.Guide`, the pages are in
   `skills/site-pages/SKILL.md`) get the page's address, what it says and the docs it came from.
   `GuideKnowsTheSiteTests` fails until they name every page.
4. **Hold it with the tests.** Add a row to `SitePagesTests.APage_IsAtItsAddress_WithItsHeading` with the page, its
   route and its heading, and one to `SiteLayoutTests.TheMenu_LeadsToThePage`. `PageDescriptionsTests`,
   `PageTitlesTests` and `SiteLinksTests` check its head and its links with nothing to add. A claim on the page about
   the repo gets a test that holds it to the repo, as `WhatIsTrueTests` does under `Pages/Claims/`.
5. **Write the sitemap again**, with `LOUIS_UPDATE_GOLDEN=1` on `SitemapTests`, and commit `wwwroot/sitemap.xml`.

## Change the look

The site decides no look of its own. `Styles/app.scss` lists only the framework's Sass and the two themes, glass and
flat, and `StylesheetTests.TheSite_HasNoStylesOfItsOwn` fails if a partial of the site's or a sheet beside one of its
components comes back. The colours are the framework's, in `src/web/LouisAgent.Web/Styles/web/settings/_colours.scss`,
so a change there reaches every site.

So a change of look is a change to the framework or a theme, made in [Change the framework's web](change-the-web.md).
Check it on this site afterwards, at 1920×1080 and at 390 px: no sideways scroll, and the menu wrapped.

## Change a film

The films are no longer on this site. They played on the framework's pages, which moved to the framework's own site,
and their markup and stylesheets went with that move.

## Export the films

`src/tools/LouisAgent.FilmExport` and its exports in `src/docs/films/` still name the old pages and the films' files,
which are gone, so the tool can't make an export until it is fixed. Its other job, the share card, still works.

## Draw the share card again

The card a link to the site shows carries the glass orb, photographed from the site itself, in the landing's brand
section at `/home/brand`. After the orb's look changes, draw it again and commit the pictures it writes:

```bash
dotnet run --project src/tools/LouisAgent.FilmExport -- social-card
```

It starts the site's host itself and needs no Docker. `CHROME_PATH` names the browser; without it, a Chrome for
Testing is downloaded once. It writes the site's `wwwroot/social-card.png` and the three copies in `src/docs/brand/`,
light and dark. A site that shares a link caches the card, so after it is live, ask Facebook's Sharing Debugger to
scrape the address again. [ADR 0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md)
says why the tool works this way.

## Change the CV

The CV at `/cv` is data, and its page is its own editor
([ADR 0122](../adr/0122-the-sites-cv-is-edited-on-its-own-page.md)). So there are two ways to change it, and neither is
the page's markup.

The first is in the admin, with no deploy. Open `/admin/resume`; the admin's key is asked for once. The page there is
the CV itself. Click any words to change them. Use the small buttons beside a link, a fact, a job or a point to move it
or take it out, and the pencil where the page draws a line put together from parts (a job's dates and company, a
school, a link's address). The panel on the right holds what the page never draws. Nothing reaches the site until you
press **Save**, and **Discard** puts the saved CV back. A save is refused, with the reason, when it holds an ID number,
a salary, a birth date or a street address.

The second is in the repository. `Content/cv.json` is the copy the site is built with, and what shows when nothing has
been saved. Edit it and deploy. It's read strictly, so a misspelt field fails `CvContentTests` rather than the page.

A copy saved in the admin wins over the built-in one. To go back to the built-in CV, delete `cv.json` from the folder
`SITE_DATA_DIRECTORY` names. To give the CV a new kind of thing, add it to its record under `Resumes/`, to
`ResumeView` where it is drawn, and there to how it is changed. The tests in `Components/Cv/Editing/` show the way.

## Test it

```bash
dotnet test src/tests/site/LouisAgent.Site.Tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/site/LouisAgent.Site.Tests     # after a deliberate markup change: then read the diff
```

The tests run in the invariant culture (`TestCulture`), so the golden files never depend on the machine. Then look at
the changed page in Chrome at 1920×1080 and at phone width, paging through each section.

---
[Guides](README.md) · [Docs index](../README.md) · Related: [LouisAgent.Site](../projects/LouisAgent.Site.md)
