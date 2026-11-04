# Change the site

> **How-to.** Running the engine's website, adding a page, changing the theme, and changing a film. The design is in
> [louis-agent.site](../projects/louis-agent.site.md).

## Run it

```bash
dotnet run --project demos/louis-agent/web/louis-agent.site          # http://localhost:5000 (or the port it prints)
dotnet test demos/louis-agent/tests/louis-agent.site.tests
docker build -f demos/louis-agent/docker/images/Dockerfile.site -t louis-agent-site . && docker run -p 5091:8080 louis-agent-site
```

## Add a page

1. A `.razor` under `Components/Pages/` with `@page "/its-route"` and a `<PageTitle>`; markup only, no `@code`.
2. Its link in `Components/Layout/SiteLayout.razor`'s menu.
3. A row in `SitePagesTests.APage_Answers_WithItsTitle` with the route and the title.
4. A new section's styles go in the file of their concern under `wwwroot/css/` (a landing part in `landing/`, a
   layout rule in `layout/`); a new file is added to `app.css`'s import list where it belongs in the cascade, and
   `SiteStylesheetTests` checks the two match.

## Change the theme

Colours, fonts and corners are tokens in `wwwroot/css/settings/tokens.css`; a new colour is a token first. The
chrome headline is `.chrome` in `base/elements.css`. Check a change at 1440 px and at 390 px (no sideways scroll).

## Change a film

A film is markup (`Components/Movies/*.razor`) and three stylesheets:

- `movie/stage.css`: what both films share (the stage, a part, the light, the card, the strip). A change here
  changes both.
- `movie/short.css` and `movie/detail.css`: one film each. Where a part stands (`.p-host { left; top; width;
  height }` on a 1280×720 floor whose centre is 640×380), the camera (`@keyframes camera-s` / `camera-d`: wide at
  the start and the end; to fly in on a part, `translate(640 − its centre x, 380 − its centre y)` and a `scale`),
  when a part lights up (`lit-*`), the light's path (`travel-*`), and when each card and caption shows (`show-*`,
  `step-*`). A scene's window is its share of the loop; keep a transition at 2 to 3% of the loop so the camera
  doesn't snap.
- A keyframe name ends in the film's letter (`-s`, `-d`, or `-s3`, `-d10`); `SiteStylesheetTests` refuses a name
  the two could share.

To add a scene: its card and caption in the markup (`c7`, `7 / 7`), a step mark (`k7`), the windows re-cut in the
film's stylesheet, the counts in `ShortMovieTests` or `DetailedMovieTests`, and the golden rewritten with
`LOUIS_UPDATE_GOLDEN=1`. To see the result frame by frame without waiting, pause the page's animations in the
browser's console and set their time: `document.getAnimations().forEach(a => { a.pause(); a.currentTime = 12000 })`.

## Export a film as a video

A headless browser steps the animations frame by frame and ffmpeg joins the frames (the script is in the project
files, `plans/website/`): 30 frames a second, 1280×720, H.264. Re-export after a change to a film.

---
[Docs index](../README.md) · Related: [Guides](README.md) · [louis-agent.site](../projects/louis-agent.site.md)
