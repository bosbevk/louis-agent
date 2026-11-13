# Change the framework's site

> **How-to.** Change `src/site/louis-agent.site`, the framework's website (louis-agent.dev): run it, add a page, change
> its look, change and export a film, and test it. **Elsewhere:** how it works in
> [louis-agent.site](../projects/louis-agent.site.md); what every site shares in
> [Change the framework's web](change-the-web.md); why it lives in `src/site/` in
> [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md).

The site is Blazor WebAssembly on the framework's web, served by its own host, `louis-agent.site-api`, whose catalogue
is the guide in the corner and nothing else.

## Run it

```bash
docker compose -f src/site/docker/docker-compose.yml up -d --build      # http://127.0.0.1:5080, the review copy
dotnet run --project src/site/louis-agent.site-api                      # http://127.0.0.1:5083
```

Its settings are `src/site/config/` ([what each holds](../../site/config/README.md)): copy `.env.secrets.example` to
`.env.secrets` and fill in `AGENT_API_KEY`, or the host refuses to start, and `ANTHROPIC_API_KEY`, or the guide can't
answer. After a change to the site, rebuild the image or restart the run.

## Add a page

1. A `.razor` under `Pages/` with `@page "/its-route"`, a `<PageTitle>` and a `<PageHead>` with its description;
   markup only, no `@code`. It draws in the site's frame by default.
2. Its link in `Layout/SiteLayout.razor`'s menu.
3. A row in `SitePagesTests.APage_IsAtItsAddress_WithItsHeading` with the route and the heading, and one in
   `SiteLayoutTests.TheMenu_LeadsToThePage`. Code or a list that mirrors the repo gets a test holding it to the repo,
   as `BuildYourOwnTests`, `PacksPageTests`, `ProofPageTests` (the rand to the rates file), `StandsOnTests` (the
   packages to the project files) and `WhatsComingTests` (the planned features to the TODO's open rows) do.
4. A new section's styles go in the partial of their concern under `Styles/site/` (a landing part in `landing/`); a
   new partial is added to `app.scss`'s list where it belongs in the cascade, and `StylesheetTests` checks the two
   match. The site's element rules hang off `.site`, the frame's root.
5. A block that should reveal as it enters gets `reveal` (and a `data-delay`); every section is a `Band` with an id in
   words, so it has its own address and the guide can take a visitor there
   ([Make a page move](change-the-web.md#make-a-page-move)).
6. The guide's voice (`src/site/louis-agent.identity.guide`, `voice.md`, *The site you sit on*) gets the page's
   address, what it says and the docs it came from: `GuideKnowsTheSiteTests` fails until it does. A new project of the
   solution has to be named on the Architecture page (`SiteMapTests`); a new identity on Identities, a new pack on
   Packs. Then regenerate the sitemap: `LOUIS_UPDATE_GOLDEN=1` on `SitemapTests`.

## Change the look

The colours and fonts are the framework's, in `src/web/louis-agent.web/Styles/web/settings/_colours.scss`, so a change
there reaches every site with none of its own (and means exporting the films). What's only the site's, such as its
corners and gutter, is in `Styles/site/settings/_tokens.scss`; the chrome headline is `.chrome` in
`Styles/site/base/_elements.scss`. Check a change at 1920×1080 and at 390 px: no sideways scroll, the menu wrapped.

## Change a film

A film is markup (`Components/Movies/*.razor`) and three stylesheets under `Styles/site/movie/`:

- `_stage.scss`: what both films share (the stage, a part, the light, the card, the strip). A change here changes both.
- `_short.scss` and `_detail.scss`: one film each. Where a part stands (`.p-host { left; top; width; height }` on a
  1280×720 floor whose centre is 640×380), the camera (`@keyframes camera-s` / `camera-d`: wide at the start and the
  end; to fly in on a part, `translate(640 − its centre x, 380 − its centre y)` and a `scale`), when a part lights up
  (`lit-*`), the light's path (`travel-*`), and when each card and caption shows (`show-*`, `step-*`). A scene's
  window is its share of the loop; keep a transition at 2 to 3% of the loop so the camera doesn't snap.
- A keyframe name ends in the film's letter (`-s`, `-d`, or `-s3`, `-d10`); `StylesheetTests` refuses a name the two
  could share.

To add a scene: its card and caption in the markup (`c7`, `7 / 7`), a step mark (`k7`), the windows re-cut in the
film's stylesheet, the counts in `ShortMovieTests` or `DetailedMovieTests`, and the golden rewritten with
`LOUIS_UPDATE_GOLDEN=1`. Then export the films (below). To see the result frame by frame without waiting, pause the
page's animations in the browser's console and set their time:
`document.getAnimations().forEach(a => { a.pause(); a.currentTime = 12000 })`.

## Export the films

After changing a film, run the tool and commit what it writes, or `ExportedFilmsTests.TheExports_MatchTheFilms` fails:

```bash
dotnet run --project src/tools/louis-agent.film-export          # both, into src/docs/films
dotnet run --project src/tools/louis-agent.film-export -- short # one
```

It needs Docker running, for the encoder. `CHROME_PATH` names the browser; without it, a Chrome for Testing is
downloaded once into the temp folder. It starts the site's host itself, from the code as it is, and takes several
minutes a film. Each re-export adds the films' size to git history, so group changes.
[ADR 0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md) says why it works this way.

## Change the CV

The CV at `/cv` is data ([ADR 0122](../adr/0122-a-page-of-data-is-one-record-and-its-own-editor.md)), so there are
two ways to change it, and neither is the page's markup.

- **In the admin, without a deploy.** Open `/admin/resume` (the admin's key is asked for once). The page there is
  the CV itself: click any words to change them, use the small buttons beside a link, a fact, a job or a point to
  move it or take it out, and the pencil where the page draws a composed line (a job's dates and company, a school,
  a link's address). The panel on the right holds what the page never draws. Nothing reaches the site until
  **Save**, and **Discard** puts the saved CV back. A save is refused, with the reason, when it holds an ID number,
  a salary, a birth date or a street address.
- **In the repository.** `Content/cv.json` is the copy the site is built with, and what shows when nothing was
  saved. Edit it and deploy. It is read strictly, so a misspelt field fails `CvContentTests`, not the page.

A copy saved in the admin wins over the built-in one. To go back to the built-in CV, delete `cv.json` from the
folder `SITE_DATA_DIRECTORY` names. To give the CV a new kind of thing, add it to its record under `Resumes/`, to
`ResumeView` where it is drawn, and there to how it is changed. The tests in `Components/Cv/Editing/` show the way.

## Test it

```bash
dotnet test src/tests/site/louis-agent.site.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/site/louis-agent.site.tests     # after a deliberate markup change: then read the diff
```

The tests run in the invariant culture (`TestCulture`), so the golden files never depend on the machine. Then look at
the changed page in Chrome at 1920×1080 and at phone width, paging through each section.

---
[Guides](README.md) · [Docs index](../README.md) · Related: [louis-agent.site](../projects/louis-agent.site.md)
